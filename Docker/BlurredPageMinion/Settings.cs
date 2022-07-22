using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlurredPageMinion
{

    public static class Settings
    {
        public static string ApiKey { get; set; } = "";
        //public static int Threads { get; set; } = 2;
        //public static bool LessResources { get; set; } = false;
        public static string Proxy { get; set; } = null;
        public static bool Debug { get; set; } = false;

        public static string Version { get; set; } = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
    }

}
