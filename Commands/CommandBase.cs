using System.CommandLine;

namespace Commands
{
    public abstract class CommandBase {
        public string DefaultDescription {get; set;} = "This is default description of command!";
        public Option<string> ApiKeyOption {get;} = new Option<string>("--key")
        {
            Description = "Your Roblox API Key or Auth Key (e.g. XXXXX::<auth/api>)",
            Required = true
        };

        // key util
        public enum KeyTypeEnum
        {
            API_KEY,
            AUTH_KEY
        }
        public record IKey
        {
            public string KeyValue {get; set;} = null!;
            public KeyTypeEnum KeyType {get; set;}
        }
        public async Task<IKey> ParseKey(string Key)
        {
            var KeySplit = Key.Split("::");
            if(KeySplit.Length < 2)
            {
                throw new Exception("Invalid API Key or Auth Key");
            }

            return new IKey
            {
                KeyValue = KeySplit[0],
                KeyType = KeySplit[1] == "auth" ? KeyTypeEnum.AUTH_KEY : KeyTypeEnum.API_KEY
            };
        }

        // blueprint
        public abstract Command Spawn(string[] Args);
    }
}