using EasyHook;
using Speedo.Interface;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;

namespace Speedo
{
    public class SpeedoProcess
    {
        private readonly string _channelName = null;
        private readonly IpcServerChannel _speedoServer;
        public SpeedoInterface SpeedoInterface;
        public Process Process;

        public SpeedoProcess(Process process, SpeedoConfig config, SpeedoInterface speedoInterface)
        {
            speedoInterface.ProcessId = process.Id;
            SpeedoInterface = speedoInterface;
            _speedoServer = RemoteHooking.IpcCreateServer(ref _channelName, WellKnownObjectMode.Singleton, SpeedoInterface);
            RemoteHooking.Inject(process.Id, InjectionOptions.Default, typeof(SpeedoInterface).Assembly.Location, null, _channelName, config);
            Process = process;
        }
    }
}
