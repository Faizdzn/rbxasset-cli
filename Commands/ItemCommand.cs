using System.CommandLine;
using Actions;

namespace Commands
{
    public class ItemCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Download roblox item on your shell" ?? DefaultDescription;

            // Option
            var ItemIdOption = new Option<int>("--item-id")
            {
               Description = "Item ID"
            };

            // Cmd
            var Cmd = new Command("item", Description)
            {
                ApiKeyOption,
                ItemIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var ItemId = act.GetValue(ItemIdOption);

                    await ItemAction.Run(await ParseKey(ApiKey ?? ""), ItemId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}