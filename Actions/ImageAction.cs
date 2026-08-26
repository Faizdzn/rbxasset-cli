using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class ImageAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, long ImageId)
        {
            var ModelApi = new RobloxModelApi(ApiKey);
            Console.WriteLine(await ModelApi.GetImageFile(ImageId));
        }
    }
}