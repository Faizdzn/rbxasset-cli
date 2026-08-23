using System.CommandLine;

namespace Commands
{
    public abstract class CommandBase {
        public string DefaultDescription {get; set;} = "This is default description of command!";
        public Option ApiKeyOption {get;} = new Option<string>("--apiKey")
        {
            Description = "Your Roblox API Key",
            Required = true
        };
        public abstract Command Spawn(string[] Args);
    }
}