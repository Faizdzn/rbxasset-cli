using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class MeshAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int MeshId)
        {
            var ModelApi = new RobloxModelApi(ApiKey);
            // Console.WriteLine(await ModelApi.GetMeshFile(MeshId));
        }
    }
}