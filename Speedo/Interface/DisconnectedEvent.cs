using System;

namespace Speedo.Interface
{
    [Serializable]
    public delegate void DisconnectedEvent();

    [Serializable]
    public class DisconnectedEventProxy : MarshalByRefObject
    {
        public event DisconnectedEvent DisconnectedEventHandler;

        public void DisconnectedEventFire()
        {
            DisconnectedEventHandler?.Invoke();
        }
    }
}
