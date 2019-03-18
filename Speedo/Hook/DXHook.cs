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
            // First we need to determine the function address for IDirect3DDevice9
            id3dDeviceFunctionAddresses = new List<IntPtr>();
            using (Device device = new Device(new Direct3D(), 0, DeviceType.NullReference, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1 }))
                id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(device.NativePointer, D3D9_DEVICE_METHOD_COUNT));

            // We want to hook each method of the IDirect3DDevice9 interface that we are interested in

            DebugMessage("Initialising hooks.");
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
            Device device = (Device)devicePtr;
            int code;
            try
            {
                lock (_lockRenderTarget)
                {
                    if (_renderTarget != null)
                    {
                        _renderTarget.Dispose();
                        _renderTarget = null;
                    }
                }
                speedo.Dispose();
                isInitialised = false;
                device.Reset(presentParameters);
                code = Result.Ok.Code;
            }
            catch (SharpDXException ex)
            {
                DebugMessage(ex.Message);
                code = ex.ResultCode.Code;
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
                code = Result.Ok.Code;
            }
            return code;
        }

        private unsafe int PresentHook(
          IntPtr devicePtr,
          Rectangle* pSourceRect,
          Rectangle* pDestRect,
          IntPtr hDestWindowOverride,
          IntPtr pDirtyRegion)
        {
            try
            {
                Device device = (Device)devicePtr;
                DoSpeedoRenderTarget(device);
                device.Present();
            }
            catch
            {
            }
            return Result.Ok.Code;
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
        private delegate int Direct3D9Device_EndSceneDelegate(IntPtr device);

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
