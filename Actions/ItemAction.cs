using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class ItemAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, long ItemId)
        {
            var AssetApi = new RobloxAssetApi(ApiKey);
            Console.WriteLine(await AssetApi.ZipItemObjToBuffer(ItemId));
        }
    }
}