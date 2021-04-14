using System.Runtime.InteropServices;
using System.Text;

public static class NativeMethods
{
    [DllImport("injector", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Inject(
        int processId,
        [MarshalAs(UnmanagedType.LPWStr)] string dllName,
        [MarshalAs(UnmanagedType.LPStr)] string exportName,
        [MarshalAs(UnmanagedType.LPWStr)] string exportArgument);


    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    internal static extern int WritePrivateProfileString(
        string section,
        string key,
        string val,
        string filePath);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    internal static extern int GetPrivateProfileString(
        string section,
        string key,
        string def,
        StringBuilder retVal,
        int size,
        string filePath);
}