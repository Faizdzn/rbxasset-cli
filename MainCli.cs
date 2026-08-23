using System.CommandLine;
using System.Reflection;
using Commands;
using Module;

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
        if(Args.Contains("--help"))
        {
            Console.WriteLine(CliHeader.MainHead());
        }

        // root command
        var RootCmd = new RootCommand(CliHeader.MainHead(false));
        RootCmd.Add(new BundleCommand().Spawn(Args));

        // execute task parse on root cmd
        return await RootCmd.Parse(Args).InvokeAsync();
    }
}