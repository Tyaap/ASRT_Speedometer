using System.Text;
using static Speedo_Loader.NativeMethods;

namespace Speedo_Loader
{
    public class IniFile
    {
        public string path;

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
