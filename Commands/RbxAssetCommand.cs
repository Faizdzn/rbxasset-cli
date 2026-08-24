using System.CommandLine;
using Actions;

namespace Commands
{
    public class RbxAssetCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Get roblox assetid detail on your shell" ?? DefaultDescription;

            // Option
            var RbxAssetIdOption = new Option<int>("--asset-id")
            {
               Description = "Asset ID"
            };

            // Cmd
            var Cmd = new Command("RbxAsset", Description)
            {
                ApiKeyOption,
                RbxAssetIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var RbxAssetId = act.GetValue(RbxAssetIdOption);

                    await RbxAssetAction.Run(await ParseKey(ApiKey ?? ""), RbxAssetId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}