using Commands;

namespace Actions
{
    public static class BundleAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int BundleId)
        {
            Console.WriteLine($"{ApiKey} {BundleId}");
        }
    }
}