using System.CommandLine;
using Actions;

namespace Commands
{
    public class RbxAssetCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Parse roblox assetid on your shell" ?? DefaultDescription;

            // Option
            var RbxAssetUrlOption = new Option<string>("--asset-url")
            {
               Description = "RbxAsset Url (e.g. rbxassetid://xxxxx)"
            };

            // Cmd
            var Cmd = new Command("RbxAsset", Description)
            {
                ApiKeyOption,
                RbxAssetUrlOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var RbxAssetUrl = act.GetValue(RbxAssetUrlOption);

                    await RbxAssetAction.Run(await ParseKey(ApiKey ?? ""), RbxAssetUrl ?? "");
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}