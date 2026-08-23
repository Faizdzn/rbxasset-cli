using System.CommandLine;
using System.Reflection;

public class MainCli
{
    public static async Task<int> Main(string[] Args)
    {
        // If no args
        if(Args.Length < 1)
        {
            Args = new[]
            {
                "--help"
            };
        }

        // Options
        var ApiKeyOption = new Option<string>("--apiKey");

        // root command
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
        var RootCmd = new RootCommand($@"Roblox Asset Downloader (by Faizdzn) - v{appVersion!.ToString(3)}")
        {
            ApiKeyOption
        };
        RootCmd.SetAction(act =>
        {
            var apiKey = act.GetValue(ApiKeyOption);
            Console.WriteLine(apiKey);
        });
        
        // execute task parse on root cmd
        return await RootCmd.Parse(Args).InvokeAsync();
    }
}