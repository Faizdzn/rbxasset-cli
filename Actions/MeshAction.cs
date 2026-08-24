using Commands;

namespace Actions
{
    public static class MeshAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int MeshId)
        {
            Console.WriteLine($"{ApiKey} {MeshId}");
        }
    }
}