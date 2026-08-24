using Commands;

namespace Actions
{
    public static class CharacterAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int UserId, string Username = "")
        {
            Console.WriteLine($"{ApiKey} {UserId} {Username}");
        }
    }
}