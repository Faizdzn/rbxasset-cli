using System.CommandLine;
using Module;

namespace Commands
{
    public class BundleCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Download Roblox Bundle by ID" ?? DefaultDescription;

            // Option
            var BundleIdOption = new Option<int>("--bundle-id")
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

            return Cmd;
        }
    }
}