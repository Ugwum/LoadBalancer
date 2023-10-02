namespace LoadBalancer
{
    public enum LoadDistributionAlgorithm
    {
        RoundRobin,
        WeightedRoundRobin,
        Random,
        LeastConnections,
        IpHash,
        LeastResponseTime,
        LeastRequests,
        ConsistentHash
    }
}