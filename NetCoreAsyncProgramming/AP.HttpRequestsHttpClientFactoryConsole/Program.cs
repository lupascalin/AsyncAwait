using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace AP.HttpRequestsHttpClientFactoryConsole
{
    // Inspired from AspNetCore.Docs samples:
    // https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/fundamentals/http-requests/samples

    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient("PollyWaitAndRetry")
                            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                            //.AddPolicyHandler(GetRetryPolicy())
                            ;

                    services.AddTransient<IGitHubService, GitHubService>();
                            
                })
                .UseConsoleLifetime();

            Console.WriteLine("Starting the host...");

            var host = builder.Build();

            var listUri = new List<string>()
            {
                "https://api.github.com/repos/dotnet/AspNetCore.Docs/branches",
                "https://api.github.com/repos/dotnet/AspNetCore/branches",
                "https://api.github.com/repos/dotnet/runtime/branches",
                "https://api.github.com/repos/dotnet/roslyn/branches",
                "https://notexists.github.com/repos/dotnet/notexists/branches"
            };
                        
            var gitHubService = host.Services.GetRequiredService<IGitHubService>();

            foreach (var requestUri in listUri)
            {
                try
                {
                    Console.WriteLine($"Get GitHub branches from {requestUri}...");

                    var gitHubBranches = await gitHubService.GetAspNetCoreDocsBranchesAsync(requestUri);

                    Console.WriteLine($"{gitHubBranches?.Count() ?? 0} GitHub Branches found at {requestUri}");

                    if (gitHubBranches != null)
                    {
                        foreach (var gitHubBranch in gitHubBranches)
                        {
                            Console.WriteLine($"- {gitHubBranch.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to load branches from GitHub at {requestUri}");
                    host.Services.GetRequiredService<ILogger<Program>>().LogError(ex, "Unable to load branches from GitHub.");
                }
            }

            Console.Read();

            return 0;
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (exception, sleepDuration, attemptNumber, context) => {
                    Console.WriteLine($"Error {exception.Result.StatusCode} => retrying in {sleepDuration}. Attemp #{attemptNumber}...");
                });
        }
    }

    public interface IGitHubService
    {
        Task<IEnumerable<GitHubBranch>?> GetAspNetCoreDocsBranchesAsync(string requestUri);
    }

    public class GitHubService : IGitHubService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubService(IHttpClientFactory httpClientFactory) =>
            _httpClientFactory = httpClientFactory;

        public async Task<IEnumerable<GitHubBranch>?> GetAspNetCoreDocsBranchesAsync(string requestUri)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    { "Accept", "application/vnd.github.v3+json" },
                    { "User-Agent", "AP.HttpRequestsHttpClientFactoryConsole" }
                }
            };

            var httpClient = _httpClientFactory.CreateClient("PollyWaitAndRetry");
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            httpResponseMessage.EnsureSuccessStatusCode();

            using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<IEnumerable<GitHubBranch>>(contentStream);
        }
    }

    public class GitHubBranch
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }        
}
