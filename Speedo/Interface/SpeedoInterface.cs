using System;

namespace Speedo.Interface
{
    [Serializable]
    public class SpeedoInterface : MarshalByRefObject
    {
        private readonly object _lock = new object();

        public int ProcessId { get; set; }

        public event MessageReceivedEvent RemoteMessage;

        public event DisconnectedEvent Disconnected;

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
            if (RemoteMessage == null)
            {
                return;
            }

            MessageReceivedEvent messageReceivedEvent = null;
            foreach (Delegate invocation in RemoteMessage.GetInvocationList())
            {
                try
                {
                    messageReceivedEvent = (MessageReceivedEvent)invocation;
                    messageReceivedEvent(eventArgs);
                }
                catch (Exception)
                {
                    RemoteMessage -= messageReceivedEvent;
                }
            }
        }

        private void SafeInvokeDisconnected()
        {
            if (Disconnected == null)
            {
                return;
            }

            DisconnectedEvent disconnectedEvent = null;
            foreach (Delegate invocation in Disconnected.GetInvocationList())
            {
                try
                {
                    disconnectedEvent = (DisconnectedEvent)invocation;
                    disconnectedEvent();
                }
                catch (Exception)
                {
                    Disconnected -= disconnectedEvent;
                }
            }
        }

        public void Ping()
        {
        }
    }
}
