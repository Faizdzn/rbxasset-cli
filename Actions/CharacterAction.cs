using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class CharacterAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int UserId, string Username = "")
        {
            var AssetApi = new RobloxAssetApi(ApiKey);
            if(UserId > 0)
            {
                Console.WriteLine(await AssetApi.ZipUserIdObjToBuffer(UserId));
            } else
            {
                Console.WriteLine(await AssetApi.ZipUserObjToBuffer(Username));
            }
        }
    }
}