using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Remoting
{
    public class EventProxy : MarshalByRefObject
    {
        public event PingEvent Ping;
        public event UpdateConfigEvent UpdateConfig;
        public event MessageReceivedEvent MessageRecieved;

        public override object InitializeLifetimeService()
        {
            //Returning null holds the object alive
            //until it is explicitly destroyed
            return null;
        }

        public void PingProxyHandler()
        {
            Ping?.Invoke();
        }

        public void UpdateConfigProxyHandler(byte[] config)
        {
            UpdateConfig?.Invoke(config);
        }

        public void MessageRecievedProxyHandler(string message)
        {
            MessageRecieved?.Invoke(message);
        }
    }
}
