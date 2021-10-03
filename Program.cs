using System;
using System.Windows.Forms;
using System.Reflection;

namespace Unstable
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            typeof(System.Globalization.CultureInfo).GetField("s_userDefaultCulture", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, System.Globalization.CultureInfo.InvariantCulture);

            bool no = true;
            if (!OpenALNamespace.OpenAL_Notification.CheckOpenAL(ref no))
                return;

            Application.Run(new Game().window);
        }
    }
}
