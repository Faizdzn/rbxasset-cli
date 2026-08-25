using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class ModelAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, long ModelId)
        {
            var ModelApi = new RobloxModelApi(ApiKey);
            // Console.WriteLine(await ModelApi.GetRbxmFile(ModelId));
        }
    }
}