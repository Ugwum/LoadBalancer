namespace LoadBalancer
{
    public class BackendServer
    {
        public Uri Uri { get; }
        public int Weight { get; }
        public int CurrentConnections { get; set; } = 0;
        public bool IsHealthy { get; set; } = true;
        public int RequestsInProgress { get; set; } = 0;

        public BackendServer(Uri uri, int weight = 1)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            Weight = weight > 0 ? weight : 1;
        }
    }
}