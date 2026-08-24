namespace Actions
{
    public static class GroupAction
    {
        public static async Task Run(string ApiKey, int GroupId)
        {
            Console.WriteLine($"{ApiKey} {GroupId}");
        }
    }
}