using System.Net;
using System.Net.Http;

namespace LoadBalancing.Implementation
{
    public class SimpleLoadBalancer : ILoadBalancer, IDisposable
    {
        private readonly List<BackendServer> backendServers;
        private int currentIndex;
        private readonly Random random = new Random();
        private readonly HttpClient httpClient;
        private readonly LoadDistributionAlgorithm distributionAlgorithm;

        private readonly IHttpClientFactory _httpClientFactory;
        private int maxRetries = 5;
        public SimpleLoadBalancer(List<BackendServer> servers, IHttpClientFactory httpClientFactory, LoadDistributionAlgorithm algorithm = LoadDistributionAlgorithm.RoundRobin)
        {
            if (servers == null || servers.Count == 0)
            {
                throw new ArgumentException("At least one backend server is required.");
            }
            _httpClientFactory = httpClientFactory;
            backendServers = servers;
            distributionAlgorithm = algorithm;
            currentIndex = -1;
            httpClient = new HttpClient();
        }

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            for (int retry = 0; retry <= maxRetries; retry++)
            {
                BackendServer selectedServer = SelectBackendServer();
                if (selectedServer == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }

                try
                {
                    using (HttpClient httpClient = _httpClientFactory.CreateClient())
                    {
                        HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                        if (response.IsSuccessStatusCode)
                        {
                            return response;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    // Handle server unavailability or other errors here
                }

                // Retry with a different server if needed
                if (retry < maxRetries)
                {
                    await Task.Delay(1000); // Add a delay before retrying
                }
            }

            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        private BackendServer SelectBackendServer()
        {
            lock (backendServers)
            {
                if (distributionAlgorithm == LoadDistributionAlgorithm.RoundRobin)
                {
                    currentIndex = (currentIndex + 1) % backendServers.Count;
                    return backendServers[currentIndex];
                }
                else if (distributionAlgorithm == LoadDistributionAlgorithm.WeightedRoundRobin)
                {
                    // Implement weighted round-robin selection
                    var weightedServers = backendServers.SelectMany(server =>
                        Enumerable.Repeat(server, server.Weight));
                    return weightedServers.ElementAt(random.Next(weightedServers.Count()));
                }
                else if (distributionAlgorithm == LoadDistributionAlgorithm.Random)
                {
                    // Randomly select a server
                    return backendServers[random.Next(backendServers.Count)];
                }
                else if (distributionAlgorithm == LoadDistributionAlgorithm.LeastConnections)
                {
                    // Select the server with the least active connections
                    var healthyServers = backendServers.Where(server => server.IsHealthy);
                    if (healthyServers.Any())
                    {
                        return healthyServers.OrderBy(server => server.CurrentConnections).First();
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (distributionAlgorithm == LoadDistributionAlgorithm.IpHash)
                {
                    // Hash the client's IP address to select a server
                    // This is a simplified example; actual IP hashing may require more complex logic
                    var clientIpAddress = GetClientIpAddress();
                    var hash = clientIpAddress.GetHashCode();
                    var index = Math.Abs(hash % backendServers.Count);
                    return backendServers[index];
                }
                else
                {
                    throw new NotSupportedException("Unsupported distribution algorithm.");
                }
            }
        }

        private string GetClientIpAddress()
        {
            // Implement logic to retrieve the client's IP address here
            // This is a simplified example; in a real application, you'd use HttpContext or other means
            return "127.0.0.1";
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}