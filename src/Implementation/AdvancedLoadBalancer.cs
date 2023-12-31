﻿using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.Implementation
{
    public class AdvancedLoadBalancer : ILoadBalancer
    {
        private readonly List<BackendServer> backendServers;
        private readonly ConsistentHash<BackendServer> consistentHash;
        private readonly Random random = new Random();
        private readonly Dictionary<LoadDistributionAlgorithm, Func<BackendServer>> algorithmMappings;
        private readonly object lockObject = new object();

        private readonly int _maxRetries = 10;
        private readonly LoadDistributionAlgorithm _algorithm;
        private int roundRobinIndex = -1;
        private IHttpClientFactory _httpClientFactory;

        public AdvancedLoadBalancer(List<BackendServer> servers, IHttpClientFactory httpClientFactory, LoadDistributionAlgorithm algorithm = LoadDistributionAlgorithm.RoundRobin)
        {
            if (servers == null || servers.Count == 0)
            {
                throw new ArgumentException("At least one backend server is required.");
            }

            backendServers = servers;
            consistentHash = new ConsistentHash<BackendServer>();
            _algorithm = algorithm;


            // Add backend servers to the consistent hash ring
            foreach (var server in backendServers)
            {
                consistentHash.AddNode(server);
            }

            // Initialize load balancing algorithm mappings
            algorithmMappings = new Dictionary<LoadDistributionAlgorithm, Func<BackendServer>>
                {
                    { LoadDistributionAlgorithm.RoundRobin, RoundRobinAlgorithm },
                    { LoadDistributionAlgorithm.WeightedRoundRobin, WeightedRoundRobinAlgorithm },
                    { LoadDistributionAlgorithm.Random, RandomAlgorithm },
                    { LoadDistributionAlgorithm.LeastConnections, LeastConnectionsAlgorithm },
                    { LoadDistributionAlgorithm.LeastResponseTime, LeastResponseTimeAlgorithm },
                    { LoadDistributionAlgorithm.LeastRequests, LeastRequestsAlgorithm },
                    { LoadDistributionAlgorithm.ConsistentHash, ConsistentHashAlgorithm }
                    // Add mappings for more algorithms
                };
        }

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            BackendServer selectedServer = SelectBackendServer(request.RequestUri.ToString(), _algorithm);
            if (selectedServer == null)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
            }
             
            var retryPolicy = Policy
                .Handle<HttpRequestException>() 
                .WaitAndRetryAsync(_maxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            try
            {
                // Perform a health check before sending the request
                if (!await CheckServerHealthAsync(selectedServer))
                {
                    // If the server is unhealthy, return an error response
                    return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
                }

                // Increment the current connections count for the selected server
                lock (lockObject)
                {
                    selectedServer.CurrentConnections++;
                    selectedServer.RequestsInProgress++;
                }

                // Use the Polly retry policy to send the request
                HttpResponseMessage response = await retryPolicy.ExecuteAsync(async () =>
                {
                    using (var httpClient = _httpClientFactory.CreateClient())
                    {
                        return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    }
                });

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
            }
            catch (HttpRequestException)
            {
                lock (lockObject)
                {
                    selectedServer.CurrentConnections--;
                    selectedServer.RequestsInProgress--;
                }
            }

            // If all retries fail, return an error response
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
        }

        private async Task<bool> CheckServerHealthAsync(BackendServer selectedServer)
        {
            try
            {
                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                {
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(selectedServer.Uri);
                    return httpResponseMessage.IsSuccessStatusCode;
                }
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        private BackendServer SelectBackendServer(string serverUrl, LoadDistributionAlgorithm algorithm)
        {
            if (algorithmMappings.TryGetValue(algorithm, out var algorithmFunc))
            {
                return algorithmFunc();
            }

            throw new ArgumentException("Unsupported load distribution algorithm.");
        }

        #region Load Balancing Algorithm implementations

        /// <summary>
        /// Implementation for Weighted Round Robin algorithm
        /// Consider the weights of servers
        /// </summary>
        /// <returns></returns>
        private BackendServer WeightedRoundRobinAlgorithm()
        {
            lock (lockObject)
            { 
                return GetWeightedRoundRobinServer();
            }
        }


        /// <summary>
        /// Implementation for Round Robin algorithm
        /// Select the next server in a circular manner
        /// </summary>
        /// <returns></returns>
        private BackendServer RoundRobinAlgorithm()
        {
            lock (lockObject)
            { 
                int currentIndex = GetNextRoundRobinIndex();
                return backendServers[currentIndex];
            }
        }

        private int GetNextRoundRobinIndex()
        {
            roundRobinIndex = (roundRobinIndex + 1) % backendServers.Count;
            return roundRobinIndex;
        }

        
        private BackendServer GetWeightedRoundRobinServer()
        {
            int totalWeight = backendServers.Sum(server => server.Weight);
            int randomValue = random.Next(totalWeight);

            int currentWeight = 0;
            foreach (var server in backendServers)
            {
                currentWeight += server.Weight;
                if (randomValue < currentWeight)
                {
                    return server;
                }
            }
            // Fallback to the first server (in case of weights not being properly set)
            return backendServers.First();
        }

        /// <summary>
        /// Implementation Random algorithm here
        /// Randomly select a server
        /// </summary>
        /// <returns></returns>
        private BackendServer RandomAlgorithm()
        {
            lock (lockObject)
            {
                
                return backendServers[random.Next(backendServers.Count)];
            }
        }

        /// <summary>
        /// Implementation for Least Connections algorithm
        /// Choose the server with the least active connections
        /// </summary>
        /// <returns></returns>
        private BackendServer LeastConnectionsAlgorithm()
        {
            lock (lockObject)
            { 
                return GetLeastConnectionsServer();
            }
        }

        private BackendServer GetLeastConnectionsServer()
        {
            int minConnections = int.MaxValue;
            BackendServer selectedServer = null;

            foreach (var server in backendServers)
            {
                if (server.CurrentConnections < minConnections)
                {
                    minConnections = server.CurrentConnections;
                    selectedServer = server;
                }
            }
            return selectedServer;
        }

        /// <summary>
        ///  Implementation for Least Response Time algorithm
        ///  Choose the server with the lowest response time
        /// </summary>
        /// <returns></returns>
        private BackendServer LeastResponseTimeAlgorithm()
        {
            lock (lockObject)
            { 
                return GetLeastResponseTimeServer();
            }
        }

        private BackendServer GetLeastResponseTimeServer()
        {
            double minResponseTime = double.MaxValue;
            BackendServer selectedServer = null;

            foreach (var server in backendServers)
            {
                if (server.IsHealthy)
                {
                    double responseTime = MeasureResponseTime(server);
                    if (responseTime < minResponseTime)
                    {
                        minResponseTime = responseTime;
                        selectedServer = server;
                    }
                }
            }

            return selectedServer;
        }

        private double MeasureResponseTime(BackendServer server)
        {
            // Implement logic to measure the response time for the server
            // This could involve sending a test request and measuring the time it takes to receive a response
            // Return the response time as a double value
            return 0.0; // Placeholder, replace with actual measurement logic
        }

        /// <summary>
        /// Implementation of Least Requests algorithm
        /// Choose the server with the least outstanding requests
        /// </summary>
        /// <returns></returns>
        private BackendServer LeastRequestsAlgorithm()
        {
            lock (lockObject)
            {
               
                return GetLeastRequestsServer();
            }
        }

        private BackendServer GetLeastRequestsServer()
        {
            int minRequests = int.MaxValue;
            BackendServer selectedServer = null;

            foreach (var server in backendServers)
            {
                if (server.IsHealthy)
                {
                    if (server.RequestsInProgress < minRequests)
                    {
                        minRequests = server.RequestsInProgress;
                        selectedServer = server;
                    }
                }
            }

            return selectedServer;
        }


        /// <summary>
        /// Implementation for Consistent Hash algorithm 
        /// Use the consistent hash ring to select a server
        /// </summary>
        /// <returns></returns>
        private BackendServer ConsistentHashAlgorithm()
        {
            lock (lockObject)
            { 
                return consistentHash.GetNode(Guid.NewGuid().ToString()); // Use a random key for consistent hashing
            }
        }
        #endregion

    }
}

