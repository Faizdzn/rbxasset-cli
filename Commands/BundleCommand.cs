using System.CommandLine;
using Actions;

namespace Commands
{
    public class BundleCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Download roblox bundle on your shell" ?? DefaultDescription;

            // Option
            var BundleIdOption = new Option<long>("--bundle-id")
            {
               Description = "Bundle ID"
            };

            // Cmd
            var Cmd = new Command("bundle", Description)
            {
                ApiKeyOption,
                BundleIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var BundleId = act.GetValue(BundleIdOption);

                    await BundleAction.Run(await ParseKey(ApiKey ?? ""), BundleId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}