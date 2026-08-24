using Commands;

namespace Actions
{
    public static class ModelAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int ModelId)
        {
            Console.WriteLine($"{ApiKey} {ModelId}");
        }
    }
}