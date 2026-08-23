using System.CommandLine;
using System.Reflection;
using Commands;
using Modules;

public class MainCli
{
    public static async Task<int> Main(string[] Args)
    {
        // If no args
        if(Args.Length < 1)
        {
            Args = new string[]
            {
                "--help"
            };
        }

        // display logo if its on help args
        if(Args.Contains("--help"))
        {
            Console.WriteLine(CliHeader.MainHead());
        }

        // root command
        var RootCmd = new RootCommand(CliHeader.MainHead(false));

        // add subcommand
        RootCmd.Add(new BundleCommand().Spawn(Args));
        RootCmd.Add(new CharacterCommand().Spawn(Args));

        // execute task parse on root cmd
        return await RootCmd.Parse(Args).InvokeAsync();
    }
}