using Commands;

namespace Actions
{
    public static class GroupAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int GroupId)
        {
            Console.WriteLine($"{ApiKey} {GroupId}");
        }
    }
}