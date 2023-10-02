namespace LoadBalancer
{
    public interface ILoadBalancer
    {
        Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request);
    }
}