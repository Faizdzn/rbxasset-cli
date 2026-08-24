using System.CommandLine;
using Actions;

namespace Commands
{
    public class GameCommand : CommandBase
    {
        public override Command Spawn(string[] Args)
        {
            // Description
            var Description = "Get roblox game detail on your shell" ?? DefaultDescription;

            // Option
            var UniverseIdOption = new Option<int>("--universe-id")
            {
               Description = "Universe ID"
            };

            // Cmd
            var Cmd = new Command("game", Description)
            {
                ApiKeyOption,
                UniverseIdOption
            };

            // Action
            Cmd.SetAction(async(act) =>
            {
                var ApiKey = act.GetValue(ApiKeyOption);
                var UniverseId = act.GetValue(UniverseIdOption);

                await GameAction.Run(ApiKey ?? "", UniverseId);
            });

            return Cmd;
        }
    }
}