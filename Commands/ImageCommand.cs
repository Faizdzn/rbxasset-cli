using System.CommandLine;
using Actions;

namespace Commands
{
    public class ImageCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Download roblox image on your shell" ?? DefaultDescription;

            // Option
            var ImageIdOption = new Option<long>("--image-id")
            {
               Description = "Image ID"
            };

            // Cmd
            var Cmd = new Command("image", Description)
            {
                ApiKeyOption,
                ImageIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var ImageId = act.GetValue(ImageIdOption);

                    await ImageAction.Run(await ParseKey(ApiKey ?? ""), ImageId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}