namespace Actions
{
    public static class BundleAction
    {
        public static async Task Run(string ApiKey, int BundleId)
        {
            Console.WriteLine($"{ApiKey} {BundleId}");
        }
    }
}