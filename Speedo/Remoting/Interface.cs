using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using System.Windows.Forms;

namespace Remoting
{
    [Serializable]
    public delegate void PingEvent();
    [Serializable]
    public delegate void PingTimeoutEvent();
    [Serializable]
    public delegate void MessageReceivedEvent(string message);
    [Serializable]
    public delegate void UpdateConfigEvent(byte[] serialisedConfig);

    public enum MessageType
    {
        Debug,
        Information,
        Warning,
        Error,
    }

    [Serializable]
    public class Interface : MarshalByRefObject
    {
        public int ProcessId;
        public event PingEvent PingEventHandler;
        public event PingTimeoutEvent PingTimeoutEventHandler;
        public event MessageReceivedEvent MessageRecievedEventHandler;
        public event UpdateConfigEvent UpdateConfigEventHandler;

        public void RegisterEventProxy(EventProxy eventProxy)
        {
            // remove any existing copies
            PingEventHandler -= eventProxy.PingProxyHandler;
            MessageRecievedEventHandler -= eventProxy.MessageRecievedProxyHandler;
            UpdateConfigEventHandler -= eventProxy.UpdateConfigProxyHandler;

            PingEventHandler += eventProxy.PingProxyHandler;
            MessageRecievedEventHandler += eventProxy.MessageRecievedProxyHandler;
            UpdateConfigEventHandler += eventProxy.UpdateConfigProxyHandler;
        }

        public void Message(MessageType messageType, string message, params object[] args)
        {
            if (MessageRecievedEventHandler != null)
            {
                MessageReceivedEvent messageReceivedEvent = null;
                foreach (Delegate invocation in MessageRecievedEventHandler.GetInvocationList())
                {
                    try
                    {
                        messageReceivedEvent = (MessageReceivedEvent)invocation;
                        messageReceivedEvent(string.Format("{0}: {1}", messageType, string.Format(message, args)));
                    }
                    catch
                    {
                        MessageRecievedEventHandler -= messageReceivedEvent;
                    }
                }
            }
        }

        public void UpdateConfig(byte[] config)
        {
            if (UpdateConfigEventHandler != null)
            {
                UpdateConfigEvent updateConfigEvent = null;
                foreach (Delegate invocation in UpdateConfigEventHandler.GetInvocationList())
                {
                    try
                    {
                        updateConfigEvent = (UpdateConfigEvent)invocation;
                        updateConfigEvent(config);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("UpdateConfig error\n" + e.ToString());
                        UpdateConfigEventHandler -= updateConfigEvent;
                    }
                }
            }
        }

        public bool Ping()
        {
            // assume the remote process registers an event proxy, check for the ping invocation
            if (PingEventHandler != null)
            {
                foreach (Delegate invocation in PingEventHandler.GetInvocationList())
                {
                    PingEvent pingEvent = null;
                    try
                    {
                        pingEvent = (PingEvent)invocation;
                        pingEvent();
                        return true; // ping invocation found and invocation succeeded
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ping error\n" + e.ToString());
                        PingEventHandler -= pingEvent;
                    }
                }
            }
            return false; // ping invocation not found, or it was found but the invocation failed
        }
    }
}
