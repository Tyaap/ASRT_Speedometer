using EasyHook;
using SharpDX.Direct3D9;
using Speedo.Interface;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;

namespace Speedo.Hook
{
    internal class DXHook : IDisposable
    {
        private int _processId = 0;
        public SpeedoInterface Interface;
        public SpeedoConfig Config;
        private LocalHook Direct3DDevice_ResetHook = null;
        private LocalHook Direct3DDevice_PresentHook = null;
        private LocalHook Direct3DDevice_EndSceneHook = null;
        private Direct3D9Device_ResetDelegate Direct3DDevice_ResetOriginal = null;
        private Direct3D9Device_PresentDelegate Direct3DDevice_PresentOriginal = null;
        private Direct3D9Device_EndSceneDelegate Direct3DDevice_EndSceneOriginal = null;
        private bool isInitialised = false;
        private List<IntPtr> id3dDeviceFunctionAddresses = new List<IntPtr>();
        private const int D3D9_DEVICE_METHOD_COUNT = 119;
        private Speed Speed;
        private Speedometer speedo;
        private bool isUsingPresent = false;
        public delegate void OnOutputDebugStringEvent(int pid, string text);

        public DXHook(SpeedoInterface ssInterface)
        {
            Interface = ssInterface;

            try
            {
                Speed = new Speed(ProcessId);
            }
            catch (Exception ex)
            {
                DebugMessage("Failed to read pointer! " + ex.Message);
            }

            DebugMonitor.Start();
            DebugMonitor.OnOutputDebugStringHandler += new EventHandler<OnOutputDebugStringEventArgs>(OnOutputDebugString);
        }

        public void Hook()
        {
            DebugMessage("Begin Direct3D9 hook.");

            DebugMessage("Determining function address for Direct3D9Device.");
            // First we need to determine the function address for IDirect3DDevice9 and 
            id3dDeviceFunctionAddresses = new List<IntPtr>();

            using (Device device = new Device(new Direct3D(), 0, DeviceType.NullReference, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1 }))
            {
                id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(device.NativePointer, D3D9_DEVICE_METHOD_COUNT));
            }

            // We want to hook IDirect3DDevice9::Present, IDirect3DDevice9::Reset and IDirect3DDevice9Ex::EndScene

            // Get the original functions
            Direct3DDevice_ResetOriginal = (Direct3D9Device_ResetDelegate)Marshal.GetDelegateForFunctionPointer(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset], typeof(Direct3D9Device_ResetDelegate));
            Direct3DDevice_PresentOriginal = (Direct3D9Device_PresentDelegate)Marshal.GetDelegateForFunctionPointer(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present], typeof(Direct3D9Device_PresentDelegate));
            Direct3DDevice_EndSceneOriginal = (Direct3D9Device_EndSceneDelegate)Marshal.GetDelegateForFunctionPointer(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.EndScene], typeof(Direct3D9Device_EndSceneDelegate));

            DebugMessage("Initialising hook.");

            // Hook Present

            Direct3DDevice_PresentHook = LocalHook.Create(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present],
                new Direct3D9Device_PresentDelegate(PresentHook),
                this);

            // Hook Reset
            Direct3DDevice_ResetHook = LocalHook.Create(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset],
                new Direct3D9Device_ResetDelegate(ResetHook),
                this);

            // Hook EndScene
            Direct3DDevice_EndSceneHook = LocalHook.Create(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.EndScene],
                new Direct3D9Device_EndSceneDelegate(EndSceneHook),
                this);

            /*
             * Don't forget that all hooks will start deactivated...
             * The following ensures that all threads are intercepted:
             * Note: you must do this for each hook.
             */

            DebugMessage("Hooking Direct3DDevice9::Present");
            Direct3DDevice_PresentHook.ThreadACL.SetExclusiveACL(new int[1]);

            DebugMessage("Hooking Direct3DDevice9::Reset");
            Direct3DDevice_ResetHook.ThreadACL.SetExclusiveACL(new int[1]);

            DebugMessage("Hooking Direct3DDevice9::EndScene");
            Direct3DDevice_EndSceneHook.ThreadACL.SetExclusiveACL(new int[1]);

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
                    Direct3DDevice_PresentHook.Dispose();
                    Direct3DDevice_ResetHook.Dispose();
                    Direct3DDevice_EndSceneHook.Dispose();
                    speedo.Dispose();
                    Speed.Dispose();
                    DebugMonitor.Dispose();

                    isInitialised = false;
                }
                catch
                {
                }
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
            isUsingPresent = true;
            DoSpeedoRenderTarget((Device)devicePtr);
            return Direct3DDevice_PresentOriginal(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
        }

        private int EndSceneHook(IntPtr devicePtr)
        {
            if (!isUsingPresent && (int)HookRuntimeInfo.ReturnAddress != 0x447F31)
            {
                DoSpeedoRenderTarget((Device)devicePtr);
            }

            return Direct3DDevice_EndSceneOriginal(devicePtr);
        }

        private void DoSpeedoRenderTarget(Device device)
        {
            Speed.Frame();
            try
            {
                if (Speed.Display || Config.AlwaysShow)
                {
                    if (!isInitialised)
                    {
                        speedo = new Speedometer(device, (float)Config.Scale, Config.PosX, Config.PosY);
                        isInitialised = true;
                    }

                    switch (Speed.CurrentMode)
                    {
                        case Speed.CurrentModeEnum.Car:
                            speedo.Draw(Speed.Car, Speed.CurrentMode);
                            break;
                        case Speed.CurrentModeEnum.Boat:
                            speedo.Draw(Speed.Boat, Speed.CurrentMode);
                            break;
                        case Speed.CurrentModeEnum.Plane:
                            speedo.Draw(Speed.Plane, Speed.CurrentMode);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
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

        protected void DebugMessage(string message)
        {
            try
            {
                Interface.Message(MessageType.Debug, "DXHook: " + message);
            }
            catch (RemotingException)
            {
            }
        }

        private void OnOutputDebugString(object sender, OnOutputDebugStringEventArgs e)
        {
            if (e.text.Contains("GOING TO STATE:9"))
            {
                Speed = new Speed(ProcessId);
                DebugMessage(string.Format("Addresses found: {0:X8}, {1:X8}, {2:X8}", Speed.GetCarPointer(), Speed.GetBoatPointer(), Speed.GetPlanePointer()));
                Speed.Display = true;
            }

            if ((e.text.Contains("GOING TO STATE:11") || e.text.Contains("Driver::RemoveFromWorld()")) && !Config.AlwaysShow)
            {
                Speed.Display = false;
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

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_ResetDelegate(
          IntPtr device,
          ref PresentParameters presentParameters);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_PresentDelegate(
          IntPtr devicePtr,
          IntPtr pSourceRect,
          IntPtr pDestRect,
          IntPtr hDestWindowOverride,
          IntPtr pDirtyRegion);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_EndSceneDelegate(IntPtr device);
    }
}
