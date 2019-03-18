using System.Runtime.InteropServices;
using System.Text;

namespace Speedo_Loader
{
    public class IniFile
    {
        public string path;

        [DllImport("kernel32.dll")]
        private static extern long WritePrivateProfileString(
          string section,
          string key,
          string val,
          string filePath);

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(
          string section,
          string key,
          string def,
          StringBuilder retVal,
          int size,
          string filePath);

        public IniFile(string INIPath)
        {
            path = INIPath;
        }

        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, path);
        }

        public string IniReadValue(string Section, string Key)
        {
            StringBuilder retVal = new StringBuilder(byte.MaxValue);
            GetPrivateProfileString(Section, Key, "", retVal, byte.MaxValue, path);
            return retVal.ToString();
        }
    }
}
