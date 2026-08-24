using System.CommandLine;
using Actions;

namespace Commands
{
    public class UserCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Get roblox user detail on your shell" ?? DefaultDescription;

            // Option
            var UserIdOption = new Option<int>("--user-id")
            {
               Description = "User ID"
            };

            // Cmd
            var Cmd = new Command("user", Description)
            {
                ApiKeyOption,
                UserIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                try
                {                    
                    var ApiKey = act.GetValue(ApiKeyOption);
                    var UserId = act.GetValue(UserIdOption);

                    await UserAction.Run(await ParseKey(ApiKey ?? ""), UserId);
                } catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            });

            return Cmd;
        }
    }
}