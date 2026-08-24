using Commands;

namespace Actions
{
    public static class UserAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int UserId)
        {
            Console.WriteLine($"{ApiKey} {UserId}");
        }
    }
}