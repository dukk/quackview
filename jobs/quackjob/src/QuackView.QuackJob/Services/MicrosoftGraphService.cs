using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IMicrosoftGraphService
{
    //Task ClearCacheAsync();
    //Task GetCachePathAsync();
    Task<T> ExecuteInContextAsync<T>(Func<GraphServiceClient, Task<T>> action, string? accountUserName = null, string[]? scopes = null);
}

internal class MicrosoftGraphService(ILogger<MicrosoftGraphService> logger, IAlertService alertService,
    ISpecialDirectories specialDirectories) : IMicrosoftGraphService
{
    private const string DefaultClientID = "27bc410e-75a4-4bdc-9281-921f446aef52";
    private static readonly string[] DefaultClientScopes = new string[] { "User.Read" };
    private const string DefaultAccountName = "_default_";

    private readonly ILogger<MicrosoftGraphService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IAlertService alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
    private readonly ISpecialDirectories specialDirectories = specialDirectories ?? throw new ArgumentNullException(nameof(specialDirectories));

    public async Task<T> ExecuteInContextAsync<T>(Func<GraphServiceClient, Task<T>> action, string? accountUserName = null, string[]? scopes = null)
    {
        accountUserName ??= MicrosoftGraphService.DefaultAccountName;

        var clientId = MicrosoftGraphService.DefaultClientID;
        var pcaBuilder = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                .WithRedirectUri("http://localhost");
        var pca = pcaBuilder.Build();
        var auth = await this.GetAccessToken(pca, accountUserName, scopes);
        var httpClient = new HttpClient();
        var clientRequestId = Guid.NewGuid().ToString();
        
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        httpClient.DefaultRequestHeaders.Add("client-request-id", clientRequestId);

        var graphClient = new GraphServiceClient(httpClient);

        this.logger.LogDebug("Executing action in Graph context for account: {Account} using client-request-id: {ClientRequestId}",
            accountUserName, clientRequestId);

        return await action(graphClient);
    }

    protected virtual async Task<AuthenticationResult> GetAccessToken(IPublicClientApplication pca,
        string? accountUserName = null, string[]? scopes = null)
    {
        scopes ??= MicrosoftGraphService.DefaultClientScopes;

        var cacheDir = await this.specialDirectories.GetSecretsDirectoryPathAsync();
        var cacheFile = String.IsNullOrWhiteSpace(accountUserName)
            ? "msal_graph_tokens.secret"
            : $"msal_graph_tokens_{accountUserName}.secret".Replace('@', '_');
        var storageProperties = new StorageCreationPropertiesBuilder(cacheFile, cacheDir).Build();
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

        cacheHelper.RegisterCache(pca.UserTokenCache);

        AuthenticationResult? result = null;

        this.logger.LogDebug("Looking for cached graph accounts to authenticate silently");

        var accounts = await pca.GetAccountsAsync();

        foreach (var account in accounts)
        {
            var matchesRequestedAccount = accountUserName == MicrosoftGraphService.DefaultAccountName
                || account.Username.Equals(accountUserName, StringComparison.InvariantCultureIgnoreCase);

            if (!matchesRequestedAccount)
                continue;

            this.logger.LogDebug("Trying cached account: {Account}", account.Username);

            try
            {
                result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync();
                this.logger.LogInformation("Acquired token silently from cached account: {Account}", account.Username);
                break;
            }
            catch (MsalUiRequiredException)
            {
                this.logger.LogWarning("Failed to acquire token silently from cached account: {Account}", account.Username);
            }
        }

        if (result == null)
        {
            this.logger.LogWarning("Silent token acquisition failed. Falling back to device code flow.");

            result = await pca.AcquireTokenWithDeviceCode(scopes, callback =>
            {
                this.logger.LogCritical(callback.Message);

                alertService.AddAlertAsync(new Alert()
                {
                    Title = "Device Authentication Code",
                    Message = callback.Message,
                    Expires = DateTime.UtcNow.AddMinutes(15)
                });

                return Task.CompletedTask;
            }).ExecuteAsync();
        }

        if (result == null)
            throw new Exception("Unable to authenticate to the Microsoft Graph.");

        return result;
    }
}