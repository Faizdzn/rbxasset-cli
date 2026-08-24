using System.CommandLine;
using Actions;

namespace Commands
{
    public class GroupCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Get roblox group detail on your shell" ?? DefaultDescription;

            // Option
            var GroupIdOption = new Option<int>("--group-id")
            {
               Description = "Group ID"
            };

            // Cmd
            var Cmd = new Command("group", Description)
            {
                ApiKeyOption,
                GroupIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {                    
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var GroupId = act.GetValue(GroupIdOption);

                    await GroupAction.Run(await ParseKey(ApiKey ?? ""), GroupId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}