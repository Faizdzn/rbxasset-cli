using System.CommandLine;
using Actions;

namespace Commands
{
    public class ModelCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Download roblox model on your shell" ?? DefaultDescription;

            // Option
            var ModelIdOption = new Option<int>("--model-id")
            {
               Description = "Model ID"
            };

            // Cmd
            var Cmd = new Command("model", Description)
            {
                ApiKeyOption,
                ModelIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var ModelId = act.GetValue(ModelIdOption);

                    await ModelAction.Run(await ParseKey(ApiKey ?? ""), ModelId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}