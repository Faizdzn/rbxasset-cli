using System.Text.RegularExpressions;

namespace Modules.Roblox
{
    public class RobloxExtraUtil {
        public string getHashUrl(string hash)
        {
            // var st = 31;
            // for (int i = 0; i < hash.Length; i++)
            // {
            //     st ^= hash[i].ToString()[0];
            // }
            // return $"https://t{(st % 8).ToString()}.rbxcdn.com/{hash}";

            return hash;
        }

        public string str_replace(string find, string replace, string word)
        {
            Regex regex;
            for (int i = 0; i < find.Length; i++)
            {
                regex = new Regex(find[i]!.ToString(), RegexOptions.None);
                word = regex.Replace(word, replace[i].ToString());
            }

            return word;
        }
    }
}