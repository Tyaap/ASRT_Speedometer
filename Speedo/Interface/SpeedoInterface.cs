using System;

namespace Speedo.Interface
{
    [Serializable]
    public class SpeedoInterface : MarshalByRefObject
    {
        public int ProcessId;

        public event MessageReceivedEvent RemoteMessageEventHandler;

        public event DisconnectedEvent DisconnectedEventHandler;

        public void Disconnect()
        {
            SafeInvokeDisconnected();
        }

        public void Message(MessageType messageType, string format, params object[] args)
        {
            Message(messageType, string.Format(format, args));
        }

        public void Message(MessageType messageType, string message)
        {
            SafeInvokeMessageRecevied(new MessageReceivedEventArgs(messageType, message));
        }

        private void SafeInvokeMessageRecevied(MessageReceivedEventArgs eventArgs)
        {
            if (RemoteMessageEventHandler != null)
            {
                MessageReceivedEvent messageReceivedEvent = null;
                foreach (Delegate invocation in RemoteMessageEventHandler.GetInvocationList())
                {
                    try
                    {
                        messageReceivedEvent = (MessageReceivedEvent)invocation;
                        messageReceivedEvent(eventArgs);
                    }
                    catch (Exception)
                    {
                        RemoteMessageEventHandler -= messageReceivedEvent;
                    }
                }
            }
        }

        private void SafeInvokeDisconnected()
        {
            if (DisconnectedEventHandler != null)
            {
                DisconnectedEvent disconnectedEvent = null;
                foreach (Delegate invocation in DisconnectedEventHandler.GetInvocationList())
                {
                    try
                    {
                        disconnectedEvent = (DisconnectedEvent)invocation;
                        disconnectedEvent();
                    }
                    catch (Exception)
                    {
                        DisconnectedEventHandler -= disconnectedEvent;
                    }
                }
            }
        }

        public void Ping()
        {
        }
    }
}
