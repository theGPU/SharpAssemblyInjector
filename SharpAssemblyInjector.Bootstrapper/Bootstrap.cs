using System.Diagnostics;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace SharpAssemblyInjector.Bootstrapper
{
    public class Bootstrap
    {
        private const uint DLL_PROCESS_DETACH = 0, DLL_PROCESS_ATTACH = 1, DLL_THREAD_ATTACH = 2, DLL_THREAD_DETACH = 3;

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        public delegate int aDelegate([MarshalAs(UnmanagedType.LPWStr)] string runtimeConfigPath, IntPtr parameters, out IntPtr host_context_handle);
        public delegate int bDelegate(IntPtr host_context_handle, int type, out IntPtr dg);
        public delegate int cDelegate(IntPtr host_context_handle);

        public delegate int loadAssemblyDelegate(
            [MarshalAs(UnmanagedType.LPWStr)] string assemblyPath,
            [MarshalAs(UnmanagedType.LPWStr)] string typeName,
            [MarshalAs(UnmanagedType.LPWStr)] string methodName,
            IntPtr delegateTypeName,
            IntPtr reserved,
            out IntPtr dg
        );

        [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool Init(IntPtr hModule, uint ul_reason_for_call, IntPtr lpReserved)
        {
            if (ul_reason_for_call == DLL_PROCESS_ATTACH)
                Inject();
            return true;
        }

        public static void Inject()
        {
            var assemblyListMappedFile = MemoryMappedFile.OpenExisting("SharpAssemblyInjector");
            var accessor = assemblyListMappedFile.CreateViewAccessor();
            var contentLen = accessor.ReadInt32(0);
            var assemblyListMappedFileContent = new byte[contentLen];
            accessor.ReadArray(4, assemblyListMappedFileContent, 0, contentLen);
            Console.WriteLine($"Readed {assemblyListMappedFileContent.Length} bytes from memory-mapped assembly list");
            var assemblyList = Encoding.ASCII.GetString(assemblyListMappedFileContent).Split(Environment.NewLine).Select(x => x.Split(';')).SkipLast(1);

            var library = "hostfxr.dll";

            var process = Process.GetCurrentProcess();
            var module = process.Modules.OfType<ProcessModule>().FirstOrDefault(x => x.ModuleName == library) ?? throw new InvalidOperationException($"Cant find module with name: {library}");

            var a = NativeLibrary.GetExport(module.BaseAddress, "hostfxr_initialize_for_runtime_config");
            var b = NativeLibrary.GetExport(module.BaseAddress, "hostfxr_get_runtime_delegate");
            var c = NativeLibrary.GetExport(module.BaseAddress, "hostfxr_close");

            var ad = Marshal.GetDelegateForFunctionPointer<aDelegate>(a);
            var bd = Marshal.GetDelegateForFunctionPointer<bDelegate>(b);
            var cd = Marshal.GetDelegateForFunctionPointer<cDelegate>(c);

            foreach (var assemblyData in assemblyList)
            {
                Console.WriteLine($"Injecting {assemblyData[0]} with");
                Console.WriteLine($"Runtimeconfig path: {assemblyData[1]}");
                Console.WriteLine($"Class path {assemblyData[2]}");
                Console.WriteLine($"Method name {assemblyData[3]}");

                var ar = ad.Invoke(assemblyData[1], IntPtr.Zero, out IntPtr ctx);
                if (ar != 1 || ctx == IntPtr.Zero)
                {
                    cd.Invoke(ctx);
                    Console.WriteLine($"Init runtime config error: {ar}");
                    Console.WriteLine($"https://github.com/dotnet/runtime/blob/main/docs/design/features/host-error-codes.md");
                    return;
                }

                var br = bd.Invoke(ctx, 5, out IntPtr dg);
                if (br != 0 || dg == IntPtr.Zero)
                {
                    Console.WriteLine($"Get runtime delegate error: {br}");
                    return;
                }

                var loadAssemblyDelegate = Marshal.GetDelegateForFunctionPointer<loadAssemblyDelegate>(dg);

                var ret = loadAssemblyDelegate.Invoke(
                    assemblyData[0],
                    assemblyData[2],
                    assemblyData[3],
                    -1, //
                    IntPtr.Zero,
                    out IntPtr dg1
                );

                if (ret != 0 || dg1 == IntPtr.Zero)
                {
                    Console.WriteLine($"Entry point error: {ret}");
                    return;
                }

                var entryPointDelegate = Marshal.GetDelegateForFunctionPointer(dg1, typeof(Action));
                entryPointDelegate.DynamicInvoke();
            }
        }
    }
}
