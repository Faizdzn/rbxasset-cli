namespace Actions
{
    public static class ModelAction
    {
        public static async Task Run(string ApiKey, int ModelId)
        {
            Console.WriteLine($"{ApiKey} {ModelId}");
        }
    }
}