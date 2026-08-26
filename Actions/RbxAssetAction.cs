using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class RbxAssetAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, string AssetUrl)
        {
            var ModelApi = new RobloxModelApi(ApiKey);
            Console.WriteLine(await ModelApi.ParseRbxAssetId(AssetUrl));
        }
    }
}