using Speedo.Hook;
using Speedo.Interface;
using EasyHook;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;

namespace Speedo
{
    public class SpeedoProcess
    {
        private readonly string _channelName = null;
        private readonly IpcServerChannel _screenshotServer;
        public SpeedoInterface SpeedoInterface { get; }
        public Process Process { get; set; }

        public SpeedoProcess(Process process, SpeedoConfig config, SpeedoInterface speedoInterface)
        {
            speedoInterface.ProcessId = process.Id;
            SpeedoInterface = speedoInterface;
            _screenshotServer = RemoteHooking.IpcCreateServer(ref _channelName, WellKnownObjectMode.Singleton, SpeedoInterface);
            RemoteHooking.Inject(process.Id, InjectionOptions.Default, typeof(SpeedoInterface).Assembly.Location, typeof(SpeedoInterface).Assembly.Location, _channelName, config);
            Process = process;
        }
    }
}
