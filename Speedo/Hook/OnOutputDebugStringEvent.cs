using System;

namespace Speedo.Hook
{
    public delegate void OnOutputDebugStringEvent(int pid, string text);

    public class OnOutputDebugStringEventArgs : EventArgs
    {
        public int pid;
        public string text;

        public OnOutputDebugStringEventArgs(int pid, string text)
        {
            this.pid = pid;
            this.text = text;
        }
    }
}
