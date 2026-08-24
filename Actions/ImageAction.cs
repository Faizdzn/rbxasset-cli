using Commands;

namespace Actions
{
    public static class ImageAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int ImageId)
        {
            Console.WriteLine($"{ApiKey} {ImageId}");
        }
    }
}