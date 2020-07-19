﻿using EasyHook;
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
        private SpeedoInterface _interface;
        private DXHook _directXHook;
        private string _channelName;

        public EntryPoint(RemoteHooking.IContext context, string channelName, SpeedoConfig config)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            _channelName = channelName;
            _clientServerChannel = new IpcServerChannel(
                new Hashtable
                {
                    ["name"] = channelName,
                    ["portName"] = channelName + Guid.NewGuid().ToString("N")
                },
                new BinaryServerFormatterSinkProvider()
                {
                    TypeFilterLevel = TypeFilterLevel.Full
                });

            ChannelServices.RegisterChannel(_clientServerChannel, false);
            _interface = RemoteHooking.IpcConnectClient<SpeedoInterface>(channelName);
            MemoryHelper.Initialise();
        }

        public void Run(RemoteHooking.IContext context, string channelName, SpeedoConfig config)
        {
            _interface.Message(MessageType.Information, "Injected into process Id:{0}.", (object)RemoteHooking.GetCurrentProcessId());
            try
            {
                _directXHook = new DXHook(_interface, config);
                _directXHook.Hook();

            }
            catch (Exception ex)
            {
                _interface.Message(MessageType.Error, "An unexpected error occured: {0}", (object)ex.ToString());
            }

            while (_interface != null || _directXHook._config.Enabled)
            {
                if (_interface == null)
                {
                    try
                    {
                        _interface = RemoteHooking.IpcConnectClient<SpeedoInterface>(_channelName);
                        ChannelServices.RegisterChannel(_clientServerChannel, false);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        _interface.Ping();
                        DXHook.InitInterface(_interface);
                    }
                    catch
                    {
                        _interface = null;
                    }
                }
                Thread.Sleep(1000);
            }

            ChannelServices.UnregisterChannel(_clientServerChannel);
            _directXHook.Dispose();
            Thread.Sleep(100);
        }
    }
}