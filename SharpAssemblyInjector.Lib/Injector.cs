﻿using SharpAssemblyInjector.Lib.POCO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssemblyInjector.Lib
{
    public static class Injector
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, string lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualFree(IntPtr lpAddress, uint dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        [Flags]
        private enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000,
        }

        [Flags]
        private enum MemoryProtection
        {
            NoAccess = 0x1,
            ReadOnly = 0x2,
            ReadWrite = 0x4,
            WriteCopy = 0x8,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400,
        }

        private static MemoryMappedFile mappedFile = null;

        public static void InjectBootstrapper(Process targetProcess)
        {
            try
            {
                var dllPath = Path.GetFullPath("SharpAssemblyInjector.Bootstrapper.dll");
                if (targetProcess == null)
                {
                    Console.WriteLine("Failed to get the target process.");
                    return;
                }

                IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);

                if (hProcess == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to open the target process.");
                    return;
                }

                Console.WriteLine("Target process opened successfully.");

                IntPtr remoteThreadStart = GetProcAddress(GetModuleHandle("kernel32"), "LoadLibraryA");

                IntPtr remoteMemory = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)(dllPath.Length + 1), AllocationType.Commit, MemoryProtection.ReadWrite);

                if (remoteMemory == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to allocate remote memory.");
                    return;
                }

                Console.WriteLine("Remote memory allocated successfully.");

                int bytesWritten;
                if (!WriteProcessMemory(hProcess, remoteMemory, dllPath, (uint)(dllPath.Length + 1), out bytesWritten))
                {
                    Console.WriteLine("Failed to write to remote process memory.");
                    return;
                }

                Console.WriteLine($"Wrote {bytesWritten} bytes to remote process memory.");

                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, remoteThreadStart, remoteMemory, 0, IntPtr.Zero);

                if (hThread == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to create remote thread.");
                    return;
                }

                Console.WriteLine("Remote thread created successfully.");

                WaitForSingleObject(hThread, 0xFFFFFFFF);

                VirtualFree(remoteMemory, 0, AllocationType.Release);

                Console.WriteLine("Remote thread execution completed successfully.");

                CloseHandle(hThread);
                CloseHandle(hProcess);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not inject!", innerException: ex);
            }
        }

        public static void SetLoadAssemblyList(AssemblyDataPOCO[] assemblies)
        {
            var mappedFileBuilder = new StringBuilder();
            foreach (var assemblyData in assemblies)
                mappedFileBuilder.AppendLine(assemblyData.ToString());

            var mappedFileContent = mappedFileBuilder.ToString();
            var mappedFileContentBytes = Encoding.ASCII.GetBytes(mappedFileContent);
            mappedFile = MemoryMappedFile.CreateNew("SharpAssemblyInjector", mappedFileContentBytes.Length+4);
            using var accessor = mappedFile.CreateViewAccessor(0, mappedFileContentBytes.Length + 4, MemoryMappedFileAccess.Write);
            accessor.WriteArray(0, BitConverter.GetBytes(mappedFileContentBytes.Length).Concat(mappedFileContentBytes).ToArray(), 0, mappedFileContentBytes.Length+4);
            accessor.Dispose();
        }

        public static void DisposeAssemblyList() => mappedFile.Dispose();
    }
}
