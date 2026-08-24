using Commands;

namespace Actions
{
    public static class RbxAssetAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int AssetId)
        {
            Console.WriteLine($"{ApiKey} {AssetId}");
        }
    }
}