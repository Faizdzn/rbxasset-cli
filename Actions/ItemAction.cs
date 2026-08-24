namespace Actions
{
    public static class ItemAction
    {
        public static async Task Run(string ApiKey, int ItemId)
        {
            Console.WriteLine($"{ApiKey} {ItemId}");
        }
    }
}