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
    public delegate void MessageReceivedEvent(MessageReceivedEventArgs message);
    [Serializable]
    public delegate void UpdateConfigEvent(UpdateConfigEventArgs message);

    [Serializable]
    public class SpeedoInterface : MarshalByRefObject
    {
        public int ProcessId;
        public event PingEvent PingEventHandler;
        public event PingTimeoutEvent PingTimeoutEventHandler;
        public event MessageReceivedEvent RemoteMessageEventHandler;
        public event UpdateConfigEvent UpdateConfigEventHandler;
        public System.Timers.Timer pingTimer = new System.Timers.Timer(1500) { AutoReset = false };
        public bool clientConnected = false;

        public SpeedoInterface()
        {
            pingTimer.Elapsed += PingTimeout;
        }

        public void Message(MessageType messageType, string message, params object[] args)
        {
            if (RemoteMessageEventHandler != null)
            {
                MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(messageType, string.Format(message, args));
                MessageReceivedEvent messageReceivedEvent = null;
                foreach (Delegate invocation in RemoteMessageEventHandler.GetInvocationList())
                {
                    try
                    {
                        messageReceivedEvent = (MessageReceivedEvent)invocation;
                        messageReceivedEvent(eventArgs);
                    }
                    catch (Exception e)
                    {
                        RemoteMessageEventHandler -= messageReceivedEvent;
                    }
                }
            }
        }

        public void UpdateConfig(SpeedoConfig config)
        {
            if (UpdateConfigEventHandler != null)
            {
                UpdateConfigEventArgs eventArgs = new UpdateConfigEventArgs(config);
                UpdateConfigEvent updateConfigEvent = null;
                foreach (Delegate invocation in UpdateConfigEventHandler.GetInvocationList())
                {
                    try
                    {
                        updateConfigEvent = (UpdateConfigEvent)invocation;
                        updateConfigEvent(eventArgs);
                    }
                    catch(Exception e)
                    {
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
                    catch
                    {
                        PingTimeoutEventHandler -= pingTimeoutEvent;
                    }
                }
            }
            clientConnected = false;
        }
    }
}
