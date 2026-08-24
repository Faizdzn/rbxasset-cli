using System.CommandLine;
using Actions;

namespace Commands
{
    public class MeshCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Download roblox mesh on your shell" ?? DefaultDescription;

            // Option
            var MeshIdOption = new Option<int>("--mesh-id")
            {
               Description = "Mesh ID"
            };

            // Cmd
            var Cmd = new Command("mesh", Description)
            {
                ApiKeyOption,
                MeshIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var MeshId = act.GetValue(MeshIdOption);

                    await MeshAction.Run(await ParseKey(ApiKey ?? ""), MeshId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}