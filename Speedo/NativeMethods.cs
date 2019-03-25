using System;
using System.Runtime.InteropServices;

namespace Speedo
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr MapViewOfFile(
          IntPtr hFileMappingObject,
          uint dwDesiredAccess,
          uint dwFileOffsetHigh,
          uint dwFileOffsetLow,
          IntPtr dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool InitializeSecurityDescriptor(
          ref SECURITY_DESCRIPTOR sd,
          uint dwRevision);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool SetSecurityDescriptorDacl(
          ref SECURITY_DESCRIPTOR sd,
          bool daclPresent,
          IntPtr dacl,
          bool daclDefaulted);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateEvent(
          ref SECURITY_ATTRIBUTES sa,
          bool bManualReset,
          bool bInitialState,
          string lpName);

        [DllImport("kernel32.dll")]
        internal static extern bool PulseEvent(IntPtr hEvent);

        [DllImport("kernel32.dll")]
        internal static extern bool SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateFileMapping(
          IntPtr hFile,
          ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
          PageProtection flProtect,
          uint dwMaximumSizeHigh,
          uint dwMaximumSizeLow,
          string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32", SetLastError = true)]
        internal static extern int WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(
          int dwDesiredAccess,
          bool bInheritHandle,
          int dwProcessId);

        [DllImport("kernel32.dll")]
        internal static extern bool ReadProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          byte[] lpBuffer,
          IntPtr dwSize,
          IntPtr lpNumberOfBytesRead);
    }

    public struct SECURITY_DESCRIPTOR
    {
        public byte revision;
        public byte size;
        public short control;
        private readonly IntPtr owner;
        private readonly IntPtr group;
        private readonly IntPtr sacl;
        private readonly IntPtr dacl;
    }

    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        private readonly IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [Flags]
    public enum PageProtection : uint
    {
        NoAccess = 1,
        Readonly = 2,
        ReadWrite = 4,
        WriteCopy = 8,
        Execute = 16,
        ExecuteRead = 32,
        ExecuteReadWrite = 64,
        ExecuteWriteCopy = 128,
        Guard = 256,
        NoCache = 512,
        WriteCombine = 1024,
    }
}
