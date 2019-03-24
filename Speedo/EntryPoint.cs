using EasyHook;
using Speedo.Hook;
using Speedo.Interface;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;

namespace Speedo
{
    public class EntryPoint : IEntryPoint
    {
        private readonly IpcServerChannel _clientServerChannel;
        private readonly SpeedoInterface _interface;
        private DXHook _directXHook;
        private DisconnectedEventProxy _disconnectedEventProxy = new DisconnectedEventProxy();
        private bool hostDisconnected = false;

        public EntryPoint(RemoteHooking.IContext context, string channelName, SpeedoConfig config)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
           {
               using (Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AssemblyLoadingAndReflection." + new AssemblyName(args.Name).Name + ".dll"))
               {
                   byte[] numArray = new byte[manifestResourceStream.Length];
                   manifestResourceStream.Read(numArray, 0, numArray.Length);
                   return Assembly.Load(numArray);
               }
           };
            _interface = RemoteHooking.IpcConnectClient<SpeedoInterface>(channelName);
            _interface.Ping();
            IDictionary properties = new Hashtable
            {
                ["name"] = channelName,
                ["portName"] = channelName + Guid.NewGuid().ToString("N")
            };

            _clientServerChannel = new IpcServerChannel(properties, new BinaryServerFormatterSinkProvider()
            {
                TypeFilterLevel = TypeFilterLevel.Full
            });

            ChannelServices.RegisterChannel(_clientServerChannel, false);
        }

        public void Run(RemoteHooking.IContext context, string channelName, SpeedoConfig config)
        {
            _interface.Message(MessageType.Information, "Injected into process Id:{0}.", (object)RemoteHooking.GetCurrentProcessId());

            try
            {
                _directXHook = new DXHook(_interface) { Config = config };
                _directXHook.Hook();
                _disconnectedEventProxy.DisconnectedEventHandler += () => hostDisconnected = true;
                _interface.DisconnectedEventHandler += new DisconnectedEvent(_disconnectedEventProxy.DisconnectedEventFire);

            }
            catch (Exception ex)
            {
                _interface.Message(MessageType.Error, "An unexpected error occured: {0}", (object)ex.ToString());
            }

            try
            {
                while (!hostDisconnected)
                {
                    Thread.Sleep(1000);
                    _interface.Ping();
                }
            }
            catch { }

            _directXHook.Dispose();
            try
            {
                _interface.Message(MessageType.Information, "Disconnecting from process {0}", (object)RemoteHooking.GetCurrentProcessId());
            }
            catch { }

            ChannelServices.UnregisterChannel(_clientServerChannel);
            Thread.Sleep(100);
        }
    }
}