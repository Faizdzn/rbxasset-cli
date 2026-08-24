using Commands;

namespace Actions
{
    public static class GameAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int UniverseId)
        {
            Console.WriteLine($"{ApiKey} {UniverseId}");
        }
    }
}