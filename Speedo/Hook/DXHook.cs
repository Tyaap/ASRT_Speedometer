using EasyHook;
using SharpDX.Direct3D9;
using Speedo.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using static Speedo.NativeMethods;

namespace Speedo.Hook
{
    internal class DXHook : IDisposable
    {
        private int _processId = 0;
        public static SpeedoInterface _interface;
        public SpeedoConfig _config;
        private LocalHook Direct3DDevice_ResetHook = null;
        private LocalHook Direct3DDevice_PresentHook = null;
        private Direct3D9Device_ResetDelegate Direct3DDevice_ResetOriginal = null;
        private Direct3D9Device_PresentDelegate Direct3DDevice_PresentOriginal = null;
        private static InterfaceClientEventProxy _interfaceClientEventProxy = new InterfaceClientEventProxy();
        private bool isInitialised = false;
        private bool configUpdated = false;
        private List<IntPtr> id3dDeviceFunctionAddresses = new List<IntPtr>();
        private const int D3D9_DEVICE_METHOD_COUNT = 119;
        private Data data;
        private Speedometer speedo;
        UIntPtr myCodeAddress;
        private byte[] originalCode;
        UIntPtr processHandle;

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_ResetDelegate(IntPtr device, ref PresentParameters presentParameters);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_PresentDelegate(
          IntPtr devicePtr,
          IntPtr pSourceRect,
          IntPtr pDestRect,
          IntPtr hDestWindowOverride,
          IntPtr pDirtyRegion);

        public DXHook(SpeedoInterface ssInterface, SpeedoConfig config)
        {
            _config = config;
            InitInterface(ssInterface);
            _interfaceClientEventProxy.UpdateConfig += new UpdateConfigEvent(UpdateConfig);
        }

        public static void InitInterface(SpeedoInterface ssInterface)
        {
            _interface = ssInterface;
            _interface.UpdateConfigEventHandler -= _interfaceClientEventProxy.UpdateConfigProxyHandler;
            _interface.UpdateConfigEventHandler += _interfaceClientEventProxy.UpdateConfigProxyHandler;
        }

        public void Hook()
        {
            DebugMessage("Begin Direct3D9 hook.");

            DebugMessage("Determining function address for Direct3D9Device.");
            // First we need to determine the function address for IDirect3DDevice9 and 
            id3dDeviceFunctionAddresses = new List<IntPtr>();
            IntPtr devicePtr = Marshal.ReadIntPtr((IntPtr)0xE99054);
            id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(devicePtr, D3D9_DEVICE_METHOD_COUNT));

            // We want to hook IDirect3DDevice9::Present, IDirect3DDevice9::Reset

            // Get the original functions
            Direct3DDevice_ResetOriginal = (Direct3D9Device_ResetDelegate)Marshal.GetDelegateForFunctionPointer(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset], typeof(Direct3D9Device_ResetDelegate));
            Direct3DDevice_PresentOriginal = (Direct3D9Device_PresentDelegate)Marshal.GetDelegateForFunctionPointer(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present], typeof(Direct3D9Device_PresentDelegate));

            DebugMessage("Initialising hook.");

            // Hook Present - with improved compatability
            processHandle = OpenProcess(0x38, false, RemoteHooking.GetCurrentProcessId());
            myCodeAddress = VirtualAlloc(UIntPtr.Zero, 100, 0x3000, 0x40);
            List<byte> myCode = new List<byte>(); 
            // My code to change the call address
            myCode.Add(0x57); // push edi
            myCode.Add(0x57); // push edi
            myCode.Add(0x51); // push ecx
            myCode.AddRange(new byte[] { 0xE8, 0x05, 0x00, 0x00, 0x00 }); // call {next instruction + 6}
            myCode.Add(0xE9); // jmp ...
            myCode.AddRange(BitConverter.GetBytes(0x443e04 - ((int)myCodeAddress + myCode.Count + 4))); // ... 0x443e04
            WriteProcessMemory(processHandle, myCodeAddress, myCode.ToArray(), myCode.Count, UIntPtr.Zero);    
            Direct3DDevice_PresentHook = LocalHook.Create(
                (IntPtr)(int)myCodeAddress + myCode.Count,
                new Direct3D9Device_PresentDelegate(PresentHook),
                this);

            // Hook Reset
            Direct3DDevice_ResetHook = LocalHook.Create(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset],
                new Direct3D9Device_ResetDelegate(ResetHook),
                this);

            /*
             * Don't forget that all hooks will start deactivated...
             * The following ensures that all threads are intercepted:
             * Note: you must do this for each hook.
             */

            DebugMessage("Hooking Direct3DDevice9::Present");
            Direct3DDevice_PresentHook.ThreadACL.SetExclusiveACL(new int[1]);
            // Detour to my code
            List<byte> detour = new List<byte>();
            detour.Add(0xE9); // jmp ...
            detour.AddRange(BitConverter.GetBytes((int)myCodeAddress - 0x443E04)); // .. codeAddress
            originalCode = new byte[detour.Count];
            ReadProcessMemory(processHandle, (UIntPtr)0x443DFF, originalCode, detour.Count, UIntPtr.Zero);
            WriteProcessMemory(processHandle, (UIntPtr)0x443DFF, detour.ToArray(), detour.Count, UIntPtr.Zero);

            DebugMessage("Hooking Direct3DDevice9::Reset");
            Direct3DDevice_ResetHook.ThreadACL.SetExclusiveACL(new int[1]);

            DebugMessage("Hook complete.");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugMessage("Removing Direct3D hook.");
                try
                {
                    WriteProcessMemory(processHandle, (UIntPtr)0x443DFF, originalCode, originalCode.Length, UIntPtr.Zero);
                    System.Threading.Thread.Sleep(100);
                    VirtualFree(myCodeAddress, 100, 0x8000);
                    Direct3DDevice_PresentHook.Dispose();
                    Direct3DDevice_ResetHook.Dispose();
                    speedo.Dispose();

                    isInitialised = false;
                }
                catch { }
            }
        }

        private int ResetHook(IntPtr devicePtr, ref PresentParameters presentParameters)
        {
            speedo.Dispose();
            isInitialised = false;
            return Direct3DDevice_ResetOriginal(devicePtr, ref presentParameters);
        }

        private int PresentHook(
          IntPtr devicePtr,
          IntPtr pSourceRect,
          IntPtr pDestRect,
          IntPtr hDestWindowOverride,
          IntPtr pDirtyRegion)
        {
            if (_config.Enabled)
            {
                DoSpeedoRenderTarget((Device)devicePtr);
            }
            return Direct3DDevice_PresentOriginal(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
        }

        private void DoSpeedoRenderTarget(Device device)
        {
            try
            {
                if (!isInitialised)
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    data = new Data(processHandle);
                    speedo = new Speedometer(device, _config);
                    isInitialised = true;
                }

                if (configUpdated)
                {
                    if (speedo.Theme.Equals(_config.Theme))
                    {
                        speedo.UpdateConfig(_config);
                    }
                    else
                    {
                        speedo.Dispose();
                        speedo = new Speedometer(device, _config);
                    }
                    configUpdated = false;
                }

                data.GetData(data.GetPlayerIndex());
                if (data.racing || _config.AlwaysShow)
                {
                    speedo.Draw(data.speed, data.form, data.boostLevel, data.canStunt, data.available);
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
        }

        private void UpdateConfig(UpdateConfigEventArgs args)
        {
            using (var stream = new MemoryStream(args.Config))
            {
                _config = (SpeedoConfig)new BinaryFormatter().Deserialize(stream);
            }
            configUpdated = true;
        }

        private void PingRecieved()
        {
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            return GetVTblAddresses(pointer, 0, numberOfMethods);
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int startIndex, int numberOfMethods)
        {
            List<IntPtr> numList = new List<IntPtr>();
            IntPtr ptr = Marshal.ReadIntPtr(pointer);
            for (int index = startIndex; index < startIndex + numberOfMethods; ++index)
            {
                numList.Add(Marshal.ReadIntPtr(ptr, index * IntPtr.Size));
            }

            return numList.ToArray();
        }

        public static void DebugMessage(string message)
        {
            try
            {
                _interface.Message(MessageType.Debug, "Speedometer: " + message);
            }
            catch (RemotingException)
            {
            }
        }

        protected int ProcessId
        {
            get
            {
                if (_processId == 0)
                {
                    _processId = RemoteHooking.GetCurrentProcessId();
                }

                return _processId;
            }
        }
    }
}
