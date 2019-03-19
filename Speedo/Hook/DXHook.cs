using Speedo.Interface;
using EasyHook;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Speedo.Hook
{
    internal class DXHook : BaseDXHook
    {
        private LocalHook Direct3DDevice_ResetHook = null;
        private LocalHook Direct3DDevice_PresentHook = null;
        private LocalHook Direct3DDeviceEx_PresentExHook = null;
        private Direct3D9Device_ResetDelegate Direct3DDevice_ResetOriginal = null;
        private Direct3D9Device_PresentDelegate Direct3DDevice_PresentOriginal = null;
        private Direct3D9DeviceEx_PresentExDelegate Direct3DDeviceEx_PresentExOriginal = null;
        private readonly object _lockRenderTarget = new object();
        private bool isInitialised = false;
        private List<IntPtr> id3dDeviceFunctionAddresses = new List<IntPtr>();
        private const int D3D9_DEVICE_METHOD_COUNT = 119;
        private const int D3D9Ex_DEVICE_METHOD_COUNT = 15;
        private Speedometer speedo;
        private Surface _renderTarget;

        protected override string HookName => nameof(DXHook);

        public DXHook(SpeedoInterface ssInterface)
      : base(ssInterface)
        {
        }

        public override void Hook()
        {
            DebugMessage("Begin Direct3D9 hook.");

            DebugMessage("Determining function address for Direct3D9Device.");
            // First we need to determine the function address for IDirect3DDevice9 and 
            id3dDeviceFunctionAddresses = new List<IntPtr>();

            using (DeviceEx device = new DeviceEx(new Direct3DEx(), 0, DeviceType.NullReference, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1 }))
            {
                id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(device.NativePointer, D3D9_DEVICE_METHOD_COUNT + D3D9Ex_DEVICE_METHOD_COUNT));
            }

            // We want to hook IDirect3DDevice9::Present, IDirect3DDevice9::Reset, IDirect3DDevice9Ex::PresentEx

            // Get the original functions
            Direct3DDevice_ResetOriginal = (Direct3D9Device_ResetDelegate)Marshal.GetDelegateForFunctionPointer(id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset], typeof(Direct3D9Device_ResetDelegate));
            Direct3DDevice_PresentOriginal = (Direct3D9Device_PresentDelegate)Marshal.GetDelegateForFunctionPointer(id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present], typeof(Direct3D9Device_PresentDelegate));
            Direct3DDeviceEx_PresentExOriginal = (Direct3D9DeviceEx_PresentExDelegate)Marshal.GetDelegateForFunctionPointer(id3dDeviceFunctionAddresses[(int)Direct3DDevice9ExFunctionOrdinals.PresentEx], typeof(Direct3D9DeviceEx_PresentExDelegate));

            DebugMessage("Initialising hooks.");
            // Hook PresentEx
            unsafe
            {
                Direct3DDeviceEx_PresentExHook = LocalHook.Create(
                    id3dDeviceFunctionAddresses[(int)Direct3DDevice9ExFunctionOrdinals.PresentEx],
                    new Direct3D9DeviceEx_PresentExDelegate(PresentExHook),
                    this);
            }

            // Hook Present
            unsafe
            {
                Direct3DDevice_PresentHook = LocalHook.Create(
                    id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present],
                    new Direct3D9Device_PresentDelegate(PresentHook),
                    this);
            }

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
                  
            DebugMessage("Hooking Direct3DDevice9Ex::PresentEx");
            Direct3DDeviceEx_PresentExHook.ThreadACL.SetExclusiveACL(new int[1]);
            Hooks.Add(Direct3DDeviceEx_PresentExHook);
            
            DebugMessage("Hooking Direct3DDevice9::Present");
            Direct3DDevice_PresentHook.ThreadACL.SetExclusiveACL(new int[1]);
            Hooks.Add(Direct3DDevice_PresentHook);

            DebugMessage("Hooking Direct3DDevice9::Reset");
            Direct3DDevice_ResetHook.ThreadACL.SetExclusiveACL(new int[1]);
            Hooks.Add(Direct3DDevice_ResetHook);

            DebugMessage("Hook complete.");
        }

        public override void Cleanup()
        {
        }

        private void InitializeDrawing(Device device)
        {
            speedo = new Speedometer(device, (float)Config.Scale, Config.PosX, Config.PosY);
            isInitialised = true;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                lock (_lockRenderTarget)
                {
                    if (_renderTarget != null)
                    {
                        _renderTarget.Dispose();
                        _renderTarget = null;
                    }
                    speedo.Dispose();
                    Speed.Dispose();
                    isInitialised = false;
                }
            }
            catch
            {
            }
            base.Dispose(disposing);
        }

        private int ResetHook(IntPtr devicePtr, ref PresentParameters presentParameters)
        {
            speedo.Dispose();
            isInitialised = false;

            return Direct3DDevice_ResetOriginal(devicePtr, ref presentParameters);
        }

        private unsafe int PresentHook(
          IntPtr devicePtr,
          Rectangle* pSourceRect,
          Rectangle* pDestRect,
          IntPtr hDestWindowOverride,
          IntPtr pDirtyRegion)
        {
            DoSpeedoRenderTarget((Device)devicePtr);
            return Direct3DDevice_PresentOriginal(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
        }

        private unsafe int PresentExHook(
            IntPtr devicePtr,
            Rectangle* pSourceRect,
            Rectangle* pDestRect,
            IntPtr hDestWindowOverride,
            IntPtr pDirtyRegion,
            Present dwFlags)
        {
            DoSpeedoRenderTarget((Device)devicePtr);

            return Direct3DDeviceEx_PresentExOriginal(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion, dwFlags);
        }

        private void DoSpeedoRenderTarget(Device device)
        {
            Frame();
            try
            {
                if (!Config.ShowOverlay)
                {
                    return;
                }

                if (Config.AlwaysShow)
                {
                    Speed.Display = true;
                }

                if (Speed.Display)
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


        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_ResetDelegate(
          IntPtr device,
          ref PresentParameters presentParameters);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate int Direct3D9Device_PresentDelegate(
          IntPtr devicePtr,
          Rectangle* pSourceRect,
          Rectangle* pDestRect,
          IntPtr hDestWindowOverride,
          IntPtr pDirtyRegion);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate int Direct3D9DeviceEx_PresentExDelegate(
          IntPtr devicePtr,
          Rectangle* pSourceRect,
          Rectangle* pDestRect,
          IntPtr hDestWindowOverride,
          IntPtr pDirtyRegion,
          Present dwFlags);
    }
}
