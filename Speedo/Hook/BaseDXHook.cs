using Speedo.Interface;
using EasyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;

namespace Speedo.Hook
{
    internal abstract class BaseDXHook : IDisposable
    {
        protected readonly ClientSpeedoInterfaceEventProxy InterfaceEventProxy = new ClientSpeedoInterfaceEventProxy();
        private int _processId = 0;
        protected List<LocalHook> Hooks = new List<LocalHook>();

        public BaseDXHook(SpeedoInterface ssInterface)
        {
            Interface = ssInterface;
            Timer = new Stopwatch();
            Timer.Start();
            try
            {
                Speed = new Speed(ProcessId, Interface);
            }
            catch (Exception ex)
            {
                DebugMessage("Failed to read pointer! " + ex.Message);
            }
            DebugMonitor.Start();
            DebugMonitor.OnOutputDebugString += new OnOutputDebugStringHandler(OnOutputDebugString);
        }

        ~BaseDXHook()
        {
            Dispose(false);
        }

        private void OnOutputDebugString(int pid, string text)
        {
            if (text.Contains("GOING TO STATE:9"))
            {
                Speed = new Speed(ProcessId, Interface);
                DebugMessage(string.Format("Addresses found: {0:X8}, {1:X8}, {2:X8}", Speed.GetCarPointer(), Speed.GetBoatPointer(), Speed.GetPlanePointer()));
                Speed.Display = true;
            }
            if (!text.Contains("GOING TO STATE:11") && !text.Contains("Driver::RemoveFromWorld()") || Config.AlwaysShow)
            {
                return;
            }

            Speed.Display = false;
        }

        protected Stopwatch Timer { get; set; }

        protected Speed Speed { get; set; }

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

        protected virtual string HookName => nameof(BaseDXHook);

        protected void Frame()
        {
            Speed.Frame();
        }

        protected void DebugMessage(string message)
        {
            try
            {
                Interface.Message(MessageType.Debug, HookName + ": " + message);
            }
            catch (RemotingException)
            {
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

        protected static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int count = input.Read(buffer, 0, buffer.Length);
                if (count > 0)
                {
                    output.Write(buffer, 0, count);
                }
                else
                {
                    break;
                }
            }
        }

        protected static byte[] ReadFullStream(Stream stream)
        {
            if (stream is MemoryStream)
            {
                return ((MemoryStream)stream).ToArray();
            }

            byte[] buffer = new byte[32768];
            using (MemoryStream memoryStream = new MemoryStream())
            {
                int count;
                do
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        memoryStream.Write(buffer, 0, count);
                    }
                }
                while (count >= buffer.Length);
                return memoryStream.ToArray();
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (ImageCodecInfo imageDecoder in ImageCodecInfo.GetImageDecoders())
            {
                if (imageDecoder.FormatID == format.Guid)
                {
                    return imageDecoder;
                }
            }
            return null;
        }

        private Bitmap BitmapFromBytes(byte[] bitmapData)
        {
            using (MemoryStream memoryStream = new MemoryStream(bitmapData))
            {
                return (Bitmap)Image.FromStream(memoryStream);
            }
        }
        public SpeedoInterface Interface { get; set; }

        public SpeedoConfig Config { get; set; }

        public abstract void Hook();

        public abstract void Cleanup();

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            try
            {
                if (Hooks.Count > 0)
                {
                    foreach (LocalHook hook in Hooks)
                    {
                        hook.ThreadACL.SetInclusiveACL(new int[1]);
                    }

                    Thread.Sleep(100);
                    foreach (LocalHook hook in Hooks)
                    {
                        hook.Dispose();
                    }

                    Hooks.Clear();
                    DebugMonitor.Stop();
                    DebugMonitor.OnOutputDebugString -= new OnOutputDebugStringHandler(OnOutputDebugString);
                }
            }
            catch
            {
            }
        }
    }
}
