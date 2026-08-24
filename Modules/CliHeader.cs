using System.Reflection;

namespace Modules
{
    public static class CliHeader
    {
        public static string MainHead(bool withLogo = true)
        {
            // Header
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var Title = $"Roblox API Shell Tool - v{appVersion!.ToString(3)}";
            var Url = "(https://adhi.ski/git/rbxasset-cli)";
            var Logo = @"
.MMMMMMMMMMMMMMMMMM 
MMMMMMMMMMMMMMMMMMx 
MMMX         cMMMO  
MMMX        ;MMMK   
xMMMMMMMMMd'MMMN    
MMMMMMMMMO.MMMM     
MMMX      MMMM.     
MMMX     MMMM.      
MMMX    NMMM,       
MMMX   KMMMMMMMMMMN 
MMM   .MMMMMMMMMMMMM";
            var HeaderOrder = new []
            {
                Title,
                Url,
                withLogo ? $"{Logo}\n\n" : null
            };
            var TextHeaderString = string.Join("\n", HeaderOrder.Where(sel => sel != null));

            return TextHeaderString;
        }
    }
}