using SharpDX.Direct3D9;
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
using System.Runtime.Serialization;
using System.Reflection;

using Remoting;
using static MemoryHelper;

namespace Speedo.Hook
{
    public class DXHook : IDisposable
    {
        private delegate void FunctionDelegate();

        private Interface speedoInterface;
        private Data data;
        private Speedometer speedometer;
        private SpeedoConfig speedoConfig;
        private bool speedoConfigUpdated;

        private Device device;
        private FunctionDelegate drawOverlayFunction;
        private FunctionDelegate preResetFunction;
        private FunctionDelegate postResetFunction;
        private IntPtr drawOverlayPtr;
        private IntPtr preResetPtr;
        private IntPtr postResetPtr;
        private IntPtr presentHookPtr;
        private IntPtr preResetHookPtr;
        private IntPtr postResetHookPtr;

        public DXHook(Interface speedoInterface, EventProxy eventProxy)
        {
            this.speedoInterface = speedoInterface;
            eventProxy.UpdateConfig += UpdateConfig;
        }

        public void Hook()
        {
            try
            {
                if (ReadByte((IntPtr)0x443D40) != 0x90)
                {
                    speedoInterface.Message(MessageType.Debug, "Initialising hook environment");
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

                speedoInterface.Message(MessageType.Debug, "Enabling hooks");
                // Present hook
                presentHookPtr = EnableHook((IntPtr)0x443D41, drawOverlayPtr);
                // Pre-reset hook
                preResetHookPtr = EnableHook((IntPtr)0x443D5F, preResetPtr);
                // Post-reset hook
                postResetHookPtr = EnableHook((IntPtr)0x443D7E, postResetPtr);
            }
            catch(Exception e)
            {
                speedoInterface.Message(MessageType.Error, e.ToString());
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

        private void DrawOverlay()
        {
            //speedoInterface.Message(MessageType.Debug, "Device: " + (device == null));
            //speedoInterface.Message(MessageType.Debug, "Data: " + (data == null));
            //speedoInterface.Message(MessageType.Debug, "Speedometer: " + (speedometer == null));
            //speedoInterface.Message(MessageType.Debug, "speedoConfig.AlwaysShow={0} configUpdated={1}", speedoConfig.AlwaysShow, speedoConfigUpdated);
            try
            {
                if (device == null)
                {
                    speedoInterface.Message(MessageType.Debug, "Creating speedometer instance");
                    device = (Device)ReadIntPtr((IntPtr)0xE99054);
                    data = new Data();
                    speedometer = new Speedometer(speedoInterface, device);
                }
                if (speedoConfigUpdated)
                {
                    speedometer.UpdateConfig(speedoConfig);
                    speedoConfigUpdated = false;
                }
                data.GetData();
                if (speedoConfig != null)
                {
                    speedometer.Draw(data.racing || speedoConfig.AlwaysShow, data.available, data.speed, data.form, data.boostLevel, data.canStunt);
                }
            }
            catch (Exception e)
            {
                speedoInterface.Message(MessageType.Error, e.ToString());
            }
        }

        private void PreReset()
        {
            try
            {
                speedometer.OnLostDevice();
            }
            catch (Exception e)
            {
                speedoInterface.Message(MessageType.Error, e.ToString());
            }
        }

        private void PostReset()
        {
            try
            {
                speedometer.OnResetDevice();
            }
            catch (Exception e)
            {
                speedoInterface.Message(MessageType.Error, e.ToString());
            }
        }

        private void UpdateConfig(byte[] config)
        {
            using (var stream = new MemoryStream(config))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter() { Binder = new ExecutingAssemblyBinder() };
                speedoConfig = (SpeedoConfig)binaryFormatter.Deserialize(stream);
            }
            speedoConfigUpdated = true;
        }

        // Ensure this assembly is found during deserialisation
        sealed class ExecutingAssemblyBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return Type.GetType(typeName);
            }
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
                    speedometer.Dispose();
                }
                catch { }
            }
        }
    }
}
