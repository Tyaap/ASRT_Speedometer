using EasyHook;
using SharpDX.Direct3D9;
using Speedo.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using static Speedo.MemoryHelper;

namespace Speedo.Hook
{
    internal class DXHook : IDisposable
    {
        private SpeedoInterface _interface;
        public SpeedoConfig _config;
        private static InterfaceClientEventProxy _interfaceClientEventProxy = new InterfaceClientEventProxy();
        private Data _data;
        private Speedometer _speedo;
        private bool configUpdated = false;

        public delegate void FunctionDelegate();

        Device _device;
        FunctionDelegate drawOverlayFunction;
        FunctionDelegate preResetFunction;
        FunctionDelegate postResetFunction;
        IntPtr drawOverlayPtr;
        IntPtr preResetPtr;
        IntPtr postResetPtr;
        IntPtr presentHookPtr;
        IntPtr preResetHookPtr;
        IntPtr postResetHookPtr;

        public DXHook(SpeedoInterface speedoInterface, SpeedoConfig config)
        {
            _config = config;
            InitInterface(speedoInterface);
            _interfaceClientEventProxy.UpdateConfig += new UpdateConfigEvent(UpdateConfig);
        }

        public void InitInterface(SpeedoInterface speedoInterface)
        {
            _interface = speedoInterface;
            _interface.UpdateConfigEventHandler -= _interfaceClientEventProxy.UpdateConfigProxyHandler;
            _interface.UpdateConfigEventHandler += _interfaceClientEventProxy.UpdateConfigProxyHandler;
        }

        public void Hook()
        {    
            try
            {
                if (ReadByte((IntPtr)0x443D40) != 0x90)
                {
                    _interface.Message(MessageType.Debug, "Initialising hook environment");
                    // Initialise the hook environment - supports up to 5 sets of present, pre-reset, post-reset hooks.
                    byte[] nops = new byte[0xA5];
                    for (int i = 0; i < 0xA5; i++)
                    {
                        nops[i] = 0x90;
                    }
                    Write((IntPtr)0x443D40, nops);
                    // Present hook return
                    Write((IntPtr)0x443D5A, new byte[] { 0xE9, 0x86, 0x00, 0x00, 0x00 });
                    // Pre-reset hook call
                    Write((IntPtr)0x4436AA, new byte[] { 0xE8, 0xB0, 0x06, 0x00, 0x00 });
                    // Pre-reset hook return
                    Write((IntPtr)0x443D78, new byte[] { 0xA1, 0x54, 0x90, 0xE9, 0x00, 0xC3 });
                    // Post-reset hook call
                    Write((IntPtr)0x4436BC, new byte[] { 0xE8, 0xBD, 0x06, 0x00, 0x00 });
                    // Post-reset hook return
                    Write((IntPtr)0x443D97, new byte[] { 0xA1, 0x54, 0x90, 0xE9, 0x00, 0xC3 });
                }

                drawOverlayFunction = new FunctionDelegate(DrawOverlay);
                preResetFunction = new FunctionDelegate(PreReset);
                postResetFunction = new FunctionDelegate(PostReset);
                drawOverlayPtr = Marshal.GetFunctionPointerForDelegate(drawOverlayFunction);
                preResetPtr = Marshal.GetFunctionPointerForDelegate(preResetFunction);
                postResetPtr = Marshal.GetFunctionPointerForDelegate(postResetFunction);

                _interface.Message(MessageType.Debug, "Enabling hooks");
                // Present hook
                presentHookPtr = EnableHook((IntPtr)0x443D41, drawOverlayPtr);
                // Pre-reset hook
                preResetHookPtr = EnableHook((IntPtr)0x443D5F, preResetPtr);
                // Post-reset hook
                postResetHookPtr = EnableHook((IntPtr)0x443D7E, postResetPtr);
            }
            catch(Exception e)
            {
                _interface.Message(MessageType.Error, e.ToString());
            }
        }

        private IntPtr EnableHook(IntPtr hookListPtr, IntPtr functionPtr)
        {
            // Find a free hook slot
            while (ReadByte(hookListPtr) != 0x90)
            {
                hookListPtr += 5;
            }
            // Assembly code
            List<byte> callBytes = new List<byte>();
            callBytes.Add(0xE8); // call ...
            callBytes.AddRange(BitConverter.GetBytes(functionPtr.ToInt32() - hookListPtr.ToInt32() - 5)); // ... preResetFunction
            Write(hookListPtr, callBytes.ToArray());

            return hookListPtr;
        }

        private void DisableHook(IntPtr hookPtr)
        {
            Write(hookPtr, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    DisableHook(presentHookPtr);
                    DisableHook(preResetHookPtr);
                    DisableHook(postResetHookPtr);
                    System.Threading.Thread.Sleep(100);
                    _speedo.Dispose();
                }
                catch { }
            }
        }

        private void DrawOverlay()
        {
            try
            {
                if (_device == null)
                {
                    _interface.Message(MessageType.Debug, "Creating speedometer instance");
                    _device = (Device)ReadIntPtr((IntPtr)0xE99054);
                    _data = new Data();
                    _speedo = new Speedometer(_device, _config);
                }
                if (configUpdated)
                {
                    _speedo.UpdateConfig(_config);
                    configUpdated = false;
                }
                _data.GetData();
                if (_data.racing || _config.AlwaysShow)
                {
                    _speedo.Draw(_data.speed, _data.form, _data.boostLevel, _data.canStunt, _data.available);
                }
            }
            catch (Exception e)
            {
                _interface.Message(MessageType.Error, e.ToString());
            }
        }

        private void PreReset()
        {
            try
            {
                _speedo.OnLostDevice();
            }
            catch (Exception e)
            {
                _interface.Message(MessageType.Error, e.ToString());
            }
        }

        private void PostReset()
        {
            try
            {
                _speedo.OnResetDevice();
            }
            catch (Exception e)
            {
                _interface.Message(MessageType.Error, e.ToString());
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
    }
}
