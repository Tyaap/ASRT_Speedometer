using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Speedo.Interface
{
    [Serializable]
    public class UpdateConfigEventArgs : MarshalByRefObject
    {
        public byte[] Config;

        public UpdateConfigEventArgs(SpeedoConfig config)
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, config);
                Config = stream.ToArray();
            }
        }
    }
}