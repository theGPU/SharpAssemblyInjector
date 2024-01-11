using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpAssemblyInjector.Lib.POCO
{
    public class AssemblyDataPOCO
    {
        public string AssemblyPath { get; private set; }
        public string RuntimeConfigPath { get; private set; }
        public string ClassPath { get; private set; }
        public string EntryMethod { get; private set; }

        public AssemblyDataPOCO(string assemblyPath, string runtimeConfigPath, string classPath, string entryMethod)
        {
            AssemblyPath = assemblyPath;
            RuntimeConfigPath = runtimeConfigPath;
            ClassPath = classPath;
            EntryMethod = entryMethod;
        }

        public override string ToString() => $"{AssemblyPath};{RuntimeConfigPath};{ClassPath};{EntryMethod}";
    }
}
