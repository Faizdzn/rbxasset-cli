using Commands;

namespace Modules.Roblox
{
    public class RobloxMainApi
    {
        public HttpClient Http = new HttpClient();

        public RobloxMainApi(CommandBase.IKey Key)
        {
            // Api Key
            Http.DefaultRequestHeaders.Add(Key.KeyType == CommandBase.KeyTypeEnum.AUTH_KEY ? "Authorization" : "x-api-key", Key.KeyValue);
        }
    }
}