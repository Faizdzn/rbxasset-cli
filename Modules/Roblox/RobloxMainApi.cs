using Commands;

namespace Modules.Roblox
{
    public class RobloxMainApi : RobloxExtraUtil
    {
        private HttpClientHandler HttpHandler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };
        public HttpClient Http {get; set;}

        public RobloxMainApi(CommandBase.IKey Key)
        {
            // init Http
            Http = new HttpClient(HttpHandler);

            // Api Key
            Http.DefaultRequestHeaders.Add(Key.KeyType == CommandBase.KeyTypeEnum.AUTH_KEY ? "Authorization" : "x-api-key", Key.KeyValue);
        }
    }
}