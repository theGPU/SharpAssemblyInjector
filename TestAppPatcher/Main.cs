using System.Reflection;
using System.Runtime.InteropServices;
using TestApp;

namespace TestAppPatcher
{
    public class Main
    {
        [UnmanagedCallersOnly]
        public static int Init(IntPtr args)
        {
            Console.WriteLine("Hello from injected assembly");
            Console.WriteLine("Changing counter to 1000...");
            Program.Counter = 1000;
            return 0;
        }
    }
}
