using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace TypoDukk.Dashboard.GraphJobs.Services;

internal interface IGraphService
{
    //Task ClearCacheAsync();
    //Task GetCachePathAsync();
    Task<T> ExecuteInContextAsync<T>(Func<GraphServiceClient, Task<T>> action, string? accountUserName = null);
}

internal class GraphService : IGraphService
{
    private const string DEFAULT_CLIENT_ID = "27bc410e-75a4-4bdc-9281-921f446aef52";
    private static readonly string[] CLIENT_SCOPES = new string[] { "User.Read", "Calendars.Read" };
    private const string DEFAULT_ACCOUNT_CACHE = "default";

    private readonly ILogger<GraphService> logger;
    private readonly IProgram program;

    public GraphService(ILogger<GraphService> logger, IProgram program)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.program = program ?? throw new ArgumentNullException(nameof(program));;
    }

    public async Task<T> ExecuteInContextAsync<T>(Func<GraphServiceClient, Task<T>> action, string? accountUserName = null)
    {
        var clientId = GraphService.DEFAULT_CLIENT_ID;
        var pcaBuilder = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                .WithRedirectUri("http://localhost");
        var pca = pcaBuilder.Build();
        var auth = await this.getAccessToken(pca, accountUserName);
        var httpClient = new HttpClient();
        var clientRequestId = Guid.NewGuid().ToString();
        
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        httpClient.DefaultRequestHeaders.Add("client-request-id", clientRequestId);

        var graphClient = new GraphServiceClient(httpClient);

        this.logger.LogDebug("Executing action in Graph context for account: {Account} using client-request-id: {ClientRequestId}", accountUserName ?? GraphService.DEFAULT_ACCOUNT_CACHE, clientRequestId);

        return await action(graphClient);
    }

    private async Task<AuthenticationResult> getAccessToken(IPublicClientApplication pca,
        string? accountUserName = null)
    {
        var cacheDir = this.getCacheDirectory();
        var cacheFile = String.IsNullOrWhiteSpace(accountUserName)
            ? "msal_cache.dat"
            : $"msal_cache-{accountUserName}.dat".Replace('@', '_');
        var storageProperties = new StorageCreationPropertiesBuilder(cacheFile, cacheDir).Build();
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

        cacheHelper.RegisterCache(pca.UserTokenCache);

        AuthenticationResult? result = null;

        this.logger.LogDebug("Looking for cached graph accounts to authenticate silently");

        var accounts = await pca.GetAccountsAsync();

        foreach (var account in accounts)
        {
            var matchesRequestedAccount = accountUserName == GraphService.DEFAULT_ACCOUNT_CACHE
                || account.Username.Equals(accountUserName, StringComparison.InvariantCultureIgnoreCase);

            if (!matchesRequestedAccount)
                continue;

            this.logger.LogDebug("Trying cached account: {Account}", account.Username);

            try
            {
                result = await pca.AcquireTokenSilent(GraphService.CLIENT_SCOPES, account).ExecuteAsync();
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

            result = await pca.AcquireTokenWithDeviceCode(GraphService.CLIENT_SCOPES, callback =>
            {
                this.logger.LogCritical(callback.Message);
                return Task.CompletedTask;
            }).ExecuteAsync();
        }

        if (result == null)
            throw new Exception("Unable to authenticate to the Microsoft Graph.");

        return result;
    }

     private string getCacheDirectory()
    {
        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TDDashboard", "TDDGraphJobs", "cache");
        
        Directory.CreateDirectory(cacheDir);

        this.logger.LogDebug("Using cache directory {cacheDir}", cacheDir);

        return cacheDir;
    }
}