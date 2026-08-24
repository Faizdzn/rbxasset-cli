namespace Actions
{
    public static class UserAction
    {
        public static async Task Run(string ApiKey, int UserId)
        {
            Console.WriteLine($"{ApiKey} {UserId}");
        }
    }
}