namespace Actions
{
    public static class GameAction
    {
        public static async Task Run(string ApiKey, int UniverseId)
        {
            Console.WriteLine($"{ApiKey} {UniverseId}");
        }
    }
}