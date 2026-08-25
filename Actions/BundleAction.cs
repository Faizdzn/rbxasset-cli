using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class BundleAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, long BundleId)
        {
            var AssetApi = new RobloxAssetApi(ApiKey);
            Console.WriteLine(await AssetApi.ZipBundleObjToBuffer(BundleId));
        }
    }
}