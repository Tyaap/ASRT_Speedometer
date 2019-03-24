using System;

namespace Speedo.Interface
{
    public enum MessageType
    {
        Debug,
        Information,
        Warning,
        Error,
    }

    [Serializable]
    public delegate void MessageReceivedEvent(MessageReceivedEventArgs message);

    [Serializable]
    public class MessageReceivedEventArgs : MarshalByRefObject
    {
        public MessageType MessageType;

        public string Message;

        public MessageReceivedEventArgs(MessageType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", MessageType, Message);
        }
    }
}