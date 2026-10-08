namespace OmniRoute.Core.Configurations
{
    public class SmsGatewayOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
    }
}