using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AvatarIdDumper
{
    class inifile
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public inifile(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public void setup()
        {
            //Probably a better way to do this but im tired
            if (KeyExists("Settings", "mute")) { Main.mute = bool.Parse(Read("Settings", "mute")); } else {
                var ini = new inifile("AvaDCFG.ini");
                ini.Write("Settings", "mute", Main.mute.ToString());
                ini.Write("Settings", "mute_errors", Main.mute_errors.ToString());
                ini.Write("Settings", "debug", Main.debug.ToString());
                ini.Write("Settings", "keep_logs", Main.keep_logs.ToString());
                ini.Write("Settings", "upload_logs", Main.upload_logs.ToString());
                ini.Write("Settings", "public_only", Main.public_only.ToString());
                ini.Write("Settings", "bleeding_edge", Main.bleeding_edge.ToString());
            }
            if (KeyExists("Settings", "mute_errors")) Main.mute_errors = bool.Parse(Read("Settings", "mute_errors"));
            if (KeyExists("Settings", "debug")) Main.debug = bool.Parse(Read("Settings", "debug"));
            if (KeyExists("Settings", "keep_logs")) Main.keep_logs = bool.Parse(Read("Settings", "keep_logs"));
            if (KeyExists("Settings", "upload_logs")) Main.upload_logs = bool.Parse(Read("Settings", "upload_logs"));
            if (KeyExists("Settings", "public_only")) Main.public_only = bool.Parse(Read("Settings", "public_only"));
            if (KeyExists("Settings", "bleeding_edge")) Main.bleeding_edge = bool.Parse(Read("Settings", "bleeding_edge"));
        }
        public bool KeyExists(string Section, string Key)
        {
            return Read(Section, Key).Length > 0;
        }

        public void Write(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }
        public string Read(string Section, string Key)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }
    }
  

}