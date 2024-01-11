using SharpAssemblyInjector.Lib;
using SharpAssemblyInjector.Lib.POCO;
using System.Diagnostics;

namespace SharpAssemblyInjector.Console
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
            args = [
                "TestApp", //Process name
                @"..\\..\\..\\..\\..\\TestAppPatcher\\bin\\x64\\Debug\\net8.0\\TestAppPatcher.dll", @"..\\..\\..\\..\\..\\TestAppPatcher\\bin\\x64\\Debug\\net8.0\\TestAppPatcher.runtimeconfig.json", "TestAppPatcher.Main, TestAppPatcher", "Init" //injectable dll path, class path, method name
            ];
#endif

            var targetProcess = Process.GetProcessesByName(args[0]).First();
#if DEBUG
            var bootstrapperDebugPath = @"..\\..\\..\\..\\..\\SharpAssemblyInjector.Bootstrapper\\bin\\Release\\net8.0\\publish\\win-x64\\SharpAssemblyInjector.Bootstrapper.dll";
            File.Copy(bootstrapperDebugPath, "SharpAssemblyInjector.Bootstrapper.dll", true);
#endif
            var modules = args.Skip(1).Chunk(4).Select(x => new AssemblyDataPOCO(Path.GetFullPath(x[0]), Path.GetFullPath(x[1]), x[2], x[3])).ToArray();

            Injector.SetLoadAssemblyList(modules);
            Injector.InjectBootstrapper(targetProcess);

            System.Console.WriteLine("Waiting 30 seconds before disposing memory mapped assembly list...");
            await Task.Delay(30000);
            Injector.DisposeAssemblyList();
        }
    }
}
