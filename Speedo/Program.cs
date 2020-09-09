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

using Remoting;
using Speedo.Hook;

namespace Speedo
{
    public class Program
    {
        private const string channelName = "Speedo";
        private Interface speedoInterface;
        private EventProxy eventProxy;
        private IpcServerChannel speedoServerChannel;
        private DXHook directXHook;

        public int Run()
        {
            try
            {
                // Register IPC communications: Speedo Loader --> Speedo    
                speedoServerChannel = new IpcServerChannel(
                    new Hashtable
                    {
                        ["name"] = channelName,
                        ["portName"] = channelName + Guid.NewGuid().ToString("N")
                    },
                    new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });  
                ChannelServices.RegisterChannel(speedoServerChannel, false);

                // Retrieve IPC interface
                speedoInterface = (Interface)Activator.GetObject(
                    typeof(Interface),
                    "ipc://" + channelName + "/" + channelName);
                speedoInterface.Message(MessageType.Information, "Speedo.dll injection success");

                // Create event proxy
                eventProxy = new EventProxy();
                speedoInterface.RegisterEventProxy(eventProxy);

                // Hook
                MemoryHelper.Initialise();
                directXHook = new DXHook(speedoInterface, eventProxy);
                directXHook.Hook();

                while (true)
                {
                    GC.KeepAlive(directXHook); // GC likes to destroy directXHook, unlsess it is present in the while loop.
                    if (speedoInterface == null)
                    {
                        try
                        {
                            speedoInterface = (Interface)Activator.GetObject(
                                typeof(Interface),
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
    }
}