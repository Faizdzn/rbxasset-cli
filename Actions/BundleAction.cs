using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class BundleAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int BundleId)
        {
            var AssetApi = new RobloxAssetApi(ApiKey);
            // Console.WriteLine(await AssetApi.ZipBundleObjToBuffer(BundleId));
        }
    }
}