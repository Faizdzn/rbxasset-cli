namespace Actions
{
    public static class RbxAssetAction
    {
        public static async Task Run(string ApiKey, int AssetId)
        {
            Console.WriteLine($"{ApiKey} {AssetId}");
        }
    }
}