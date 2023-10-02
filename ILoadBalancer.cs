namespace LoadBalancing
{
    public interface ILoadBalancer
    {
        Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request);
    }
}