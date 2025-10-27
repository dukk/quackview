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

internal class MicrosoftGraphService(
    ILogger<MicrosoftGraphService> logger,
    IAlertService alertService,
    ISpecialPaths SpecialPaths)
    : IMicrosoftGraphService
{
    private const string DefaultClientID = "27bc410e-75a4-4bdc-9281-921f446aef52";
    private static readonly string[] DefaultClientScopes = new string[] { "User.Read" };
    private const string DefaultAccountName = "_default_";

    protected readonly ILogger<MicrosoftGraphService> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IAlertService AlertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));

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

        this.Logger.LogDebug("Executing action in Graph context for account: {Account} using client-request-id: {ClientRequestId} and scopes: {Scopes}",
            accountUserName, clientRequestId, string.Join(", ", scopes ?? []));

        return await action(graphClient);
    }

    protected virtual async Task<AuthenticationResult> GetAccessToken(IPublicClientApplication pca,
        string? accountUserName = null, string[]? scopes = null)
    {
        scopes ??= MicrosoftGraphService.DefaultClientScopes;
        accountUserName ??= MicrosoftGraphService.DefaultAccountName;

        var cacheDir = await this.SpecialPaths.GetSecretsDirectoryPathAsync();
        var cacheFile = String.IsNullOrWhiteSpace(accountUserName)
            ? "msal_graph_tokens.secret"
            : $"msal_graph_tokens_{accountUserName}.secret".Replace('@', '_').Replace('.', '_');
        var storagePropertiesBuilder = new StorageCreationPropertiesBuilder(cacheFile, cacheDir);

        // TODO: Add configuration around the token storage security...

        // storagePropertiesBuilder.WithUnprotectedFile();

        storagePropertiesBuilder.WithLinuxUnprotectedFile();

        // storagePropertiesBuilder.WithMacKeyChain();

        // storagePropertiesBuilder.WithLinuxKeyring("org.dukk.quackview.quackjob", "quackjob", "Quackjob Secrets",
        //     new KeyValuePair<string, string>("a", "1"), new KeyValuePair<string, string>("b", "2"));

        var storageProperties = storagePropertiesBuilder.Build();
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

        cacheHelper.RegisterCache(pca.UserTokenCache);

        this.Logger.LogDebug("Attempting to acquire token silently for account: {Account} using cache file: {CacheFile}", accountUserName, cacheFile);

        AuthenticationResult? result = null;

        this.Logger.LogDebug("Looking for cached graph accounts to authenticate silently");

        var accounts = await pca.GetAccountsAsync();

        foreach (var account in accounts)
        {
            var matchesRequestedAccount = accountUserName == MicrosoftGraphService.DefaultAccountName
                || account.Username.Equals(accountUserName, StringComparison.InvariantCultureIgnoreCase);

            if (!matchesRequestedAccount)
                continue;

            this.Logger.LogDebug("Trying cached account: {Account}", account.Username);

            try
            {
                result = await pca.AcquireTokenSilent(scopes, account).ExecuteAsync();
                this.Logger.LogInformation("Acquired token silently from cached account: {Account}", account.Username);
                break;
            }
            catch (MsalUiRequiredException)
            {
                this.Logger.LogDebug("Failed to acquire token silently from cached account: {Account}", account.Username);
            }
        }

        if (result == null)
        {
            this.Logger.LogDebug("Silent token acquisition failed. Falling back to device code flow.");

            result = await pca.AcquireTokenWithDeviceCode(scopes, callback =>
            {
                this.Logger.LogDebug(callback.Message);
                this.AlertService.AddAlertAsync(new Alert()
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