using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace ae.lib
{
    // https://github.com/markglibres/commandlineargs/blob/master/src/CommandLineArgs/Arguments.cs
    // 
    internal class Arguments
    {
        public static Dictionary<string, string> ToDictionary(string[] args)
        {
            Dictionary<string, string> _args = new Dictionary<string, string>();

            if (args != null) {
                string argKey = String.Empty;

                foreach (string arg in args)
                {
                    if (arg.StartsWith("-")) {
                        argKey = arg.Replace("-", String.Empty);
                        _args.Add(argKey, String.Empty);
                    }
                    else {
                        _args[argKey] = arg;
                    }
                }
            }

            return _args;
        }

/*
        internal static uint ComputeStringHash(string s)
        {
            uint num;
            if (s != null)
            {
                num = 2166136261U;
                for (int i = 0; i < s.Length; i++)
                {
                    num = ((uint)s[i] ^ num) * 16777619U;
                }
            }
            return num;
        }
*/
    }
}
