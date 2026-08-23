namespace Actions
{
    public static class CharacterAction
    {
        public static async Task Run(string ApiKey, int UserId, string Username = "")
        {
            Console.WriteLine($"{ApiKey} {UserId} {Username}");
        }
    }
}