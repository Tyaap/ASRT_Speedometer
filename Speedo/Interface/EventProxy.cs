using Speedo.Hook;
using System;
using System.Windows.Forms;

namespace Speedo.Interface
{
    public class EventProxy : MarshalByRefObject
    {
        public event PingEvent Ping;
        public event PingTimeoutEvent PingTimeout;
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

        public void PingTimeoutProxyHandler()
        {
            PingTimeout?.Invoke();
        }

        public void UpdateConfigProxyHandler(SpeedoConfig config)
        {
            UpdateConfig?.Invoke(config);
        }

        public void MessageRecievedProxyHandler(string message)
        {
            MessageRecieved?.Invoke(message);
        }
    }
}
