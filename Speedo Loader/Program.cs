using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace Speedo_Loader
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Application.EnableVisualStyles();
            Application.Run(new UserInterface());
        }
    }
}
