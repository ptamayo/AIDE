using Newtonsoft.Json;

namespace Aide.ApiGateway.Scaffolding
{

    public class OcelotConfig
    {
        public Route[] Routes { get; set; }
        public Globalconfiguration GlobalConfiguration { get; set; }
    }

    public class Globalconfiguration
    {
        public string BaseUrl { get; set; }
    }

    public class Route
    {
        public string DownstreamPathTemplate { get; set; }
        public string DownstreamScheme { get; set; }
        public Downstreamhostandport[] DownstreamHostAndPorts { get; set; }
        public string UpstreamPathTemplate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] UpstreamHttpMethod { get; set; }
    }

    public class Downstreamhostandport
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
}
