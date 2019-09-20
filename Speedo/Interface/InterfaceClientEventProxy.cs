using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speedo.Interface
{
    public class InterfaceClientEventProxy : MarshalByRefObject
    {
        public event UpdateConfigEvent UpdateConfig;

        public override object InitializeLifetimeService()
        {
            //Returning null holds the object alive
            //until it is explicitly destroyed
            return null;
        }

        public void UpdateConfigProxyHandler(UpdateConfigEventArgs args)
        {
            UpdateConfig?.Invoke(args);
        }
    }
}
