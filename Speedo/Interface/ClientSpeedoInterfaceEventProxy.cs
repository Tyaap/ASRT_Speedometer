using System;

namespace Speedo.Interface
{
    public class ClientSpeedoInterfaceEventProxy : MarshalByRefObject
    {
        public event DisconnectedEvent Disconnected;

        public void DisconnectedProxyHandler()
        {
            if (Disconnected == null)
            {
                return;
            }

            Disconnected();
        }
    }
}
