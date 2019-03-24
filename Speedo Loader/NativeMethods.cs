using System.Runtime.InteropServices;
using System.Text;

namespace Speedo_Loader
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int WritePrivateProfileString(
          string section,
          string key,
          string val,
          string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetPrivateProfileString(
          string section,
          string key,
          string def,
          StringBuilder retVal,
          int size,
          string filePath);
    }
}
