using Speedo.Hook;
using Speedo.Interface;
using Speedo;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Windows.Forms;
using static NativeMethods;

namespace Program
{
    public class Program
    {
        public static SpeedoConfig speedoConfig;
        public static bool configUpdated;
        public static SpeedoInterface speedoInterface;
        public static EventProxy eventProxy;

        private const string channelName = "Speedo";
        private static IpcServerChannel speedoServerChannel;
        private static DXHook directXHook;
        
        public static int Main(string pwzArgument)
        {
            try
            {
                // Make sure this assembly can be resolved during serialisation
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);

                // Register IPC communications: Speedo Loader --> Speedo    
                speedoServerChannel = new IpcServerChannel(
                    new Hashtable
                    {
                        ["name"] = channelName,
                        ["portName"] = channelName + Guid.NewGuid().ToString("N")
                    },
                    new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });  
                ChannelServices.RegisterChannel(speedoServerChannel, false);

                // Create event proxy
                eventProxy = new EventProxy();

                // Retrieve IPC interface
                speedoInterface = (SpeedoInterface)Activator.GetObject(
                    typeof(SpeedoInterface),
                    "ipc://" + channelName + "/" + channelName);
                speedoInterface.Message(MessageType.Information, "Speedo.dll injection success", GetCurrentProcessId());

                // Hook
                MemoryHelper.Initialise();
                directXHook = new DXHook();
                directXHook.Hook();
                eventProxy.UpdateConfig += UpdateConfig;

                while (true)
                {
                    if (speedoInterface == null)
                    {
                        try
                        {
                            speedoInterface = (SpeedoInterface)Activator.GetObject(
                                typeof(SpeedoInterface),
                                "ipc://" + channelName + "/" + channelName);
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show(e.ToString());
                        }
                    }
                    else
                    {
                        try
                        {
                            speedoInterface.RegisterEventProxy(eventProxy);
                            speedoInterface.Ping();
                        }
                        catch
                        {
                            speedoInterface = null;
                        }
                    }
                    Thread.Sleep(100);
                }
                ChannelServices.UnregisterChannel(speedoServerChannel);
                directXHook.Dispose();
                Thread.Sleep(100);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return 0;
        }

        private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            return System.Reflection.Assembly.GetExecutingAssembly();
        }

        public static void UpdateConfig(SpeedoConfig newConfig)
        {
            speedoConfig = newConfig;
            configUpdated = true;
        }
    }
}