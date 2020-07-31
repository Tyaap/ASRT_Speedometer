using Speedo.Hook;
using System;
using System.Timers;
using System.Windows.Forms;

namespace Speedo.Interface
{
    [Serializable]
    public delegate void PingEvent();
    [Serializable]
    public delegate void PingTimeoutEvent();
    [Serializable]
    public delegate void MessageReceivedEvent(string message);
    [Serializable]
    public delegate void UpdateConfigEvent(SpeedoConfig config);

    [Serializable]
    public class SpeedoInterface : MarshalByRefObject
    {
        public int ProcessId;
        public event PingEvent PingEventHandler;
        public event PingTimeoutEvent PingTimeoutEventHandler;
        public event MessageReceivedEvent MessageRecievedEventHandler;
        public event UpdateConfigEvent UpdateConfigEventHandler;
        public System.Timers.Timer pingTimer = new System.Timers.Timer(1500) { AutoReset = false };
        public bool clientConnected = false;

        public SpeedoInterface()
        {
            pingTimer.Elapsed += PingTimeout;
        }

        public void RegisterEventProxy(EventProxy eventProxy)
        {
            // remove any existing copies
            PingEventHandler -= eventProxy.PingProxyHandler;
            PingTimeoutEventHandler -= eventProxy.PingTimeoutProxyHandler;
            MessageRecievedEventHandler -= eventProxy.MessageRecievedProxyHandler;
            UpdateConfigEventHandler -= eventProxy.UpdateConfigProxyHandler;

            PingEventHandler += eventProxy.PingProxyHandler;
            PingTimeoutEventHandler += eventProxy.PingTimeoutProxyHandler;
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
                    catch (Exception e)
                    {
                        MessageRecievedEventHandler -= messageReceivedEvent;
                    }
                }
            }
        }

        public void UpdateConfig(SpeedoConfig config)
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
                    catch(Exception e)
                    {
                        Console.WriteLine("UpdateConfig error\n" + e.ToString());
                        UpdateConfigEventHandler -= updateConfigEvent;
                    }
                }
            }
        }

        public void Ping()
        {
            if (PingEventHandler != null)
            {
                foreach (Delegate invocation in PingEventHandler.GetInvocationList())
                {
                    PingEvent pingEvent = null;
                    try
                    {
                        pingEvent = (PingEvent)invocation;
                        pingEvent();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ping error\n" + e.ToString());
                        PingEventHandler -= pingEvent;
                    }
                }
            }

            clientConnected = true;
            pingTimer.Stop();
            pingTimer.Start();
        }

        public void PingTimeout(object source, ElapsedEventArgs e)
        {
            if (PingTimeoutEventHandler != null)
            {
                foreach (Delegate invocation in PingTimeoutEventHandler.GetInvocationList())
                {
                    PingTimeoutEvent pingTimeoutEvent = null;
                    try
                    {
                        pingTimeoutEvent = (PingTimeoutEvent)invocation;
                        pingTimeoutEvent();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("PingTimeout error\n" + ex.ToString());
                        PingTimeoutEventHandler -= pingTimeoutEvent;
                    }
                }
            }
            clientConnected = false;
        }
    }
}
