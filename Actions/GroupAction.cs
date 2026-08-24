using Commands;
using Modules.Roblox;

namespace Actions
{
    public static class GroupAction
    {
        public static async Task Run(CommandBase.IKey ApiKey, int GroupId)
        {
            var RbxAssetApi = new RobloxAssetApi(ApiKey);
            var Data = await RbxAssetApi.GetGroupDetail(GroupId);
            
            Console.WriteLine(Data);
        }
    }
}