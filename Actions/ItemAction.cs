using Commands;

namespace Actions
{
    public static class ItemAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int ItemId)
        {
            Console.WriteLine($"{ApiKey} {ItemId}");
        }
    }
}