using System.CommandLine;
using Actions;

namespace Commands
{
    public class CharacterCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Download roblox character on your shell" ?? DefaultDescription;

            // Option
            var UserIdOption = new Option<int>("--user-id")
            {
               Description = "User ID"
            };
            var UsernameOption = new Option<string>("--user-id")
            {
               Description = "Username"
            };

            // Cmd
            var Cmd = new Command("character", Description)
            {
                ApiKeyOption,
                UserIdOption,
                UsernameOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                var ApiKey = act.GetValue(ApiKeyOption);
                var UserId = act.GetValue(UserIdOption);
                var Username = act.GetValue(UsernameOption);

                await CharacterAction.Run(ApiKey ?? "", UserId, Username);
            });

            return Cmd;
        }
    }
}