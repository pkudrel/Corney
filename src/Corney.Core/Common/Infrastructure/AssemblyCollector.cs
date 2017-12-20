using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using NLog;

namespace Corney.Core.Common.Infrastructure
{
    public enum AssemblyInProject
    {
        Common = 1,
        View = 2,
        Core = 3,
        CommonDomain = 4,
        Other = 10
    }

    public class AssemblyCollector
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static readonly AssemblyCollector _instance = new AssemblyCollector();

        private readonly Dictionary<int, Assembly> _assemblies = new Dictionary<int, Assembly>();
        private int _counter = 100;

        static AssemblyCollector()
        {
        }

        private AssemblyCollector()
        {
        }

        // ReSharper disable once ConvertToAutoProperty
        public static AssemblyCollector Instance => _instance;

        public void AddAssembly(Assembly assembly, AssemblyInProject assemblyInProject)
        {
            var key = (int) assemblyInProject;
            if (_assemblies.ContainsKey(key))
                throw new DuplicateNameException($"AssemblyCollector contains key: {assemblyInProject}");
            if (_assemblies.ContainsValue(assembly))
                throw new DuplicateNameException($"AssemblyCollector assembly: {assembly.FullName}");

            _log.Debug($"Add assmebly: {assembly.FullName}");
            _assemblies.Add((int) assemblyInProject, assembly);
        }

        public void AddAssembly(Assembly assembly)
        {

            if (_assemblies.ContainsValue(assembly))
                throw new DuplicateNameException($"AssemblyCollector assmebly: {assembly.FullName}");

            _log.Debug($"Add assmebly: {assembly.FullName}");
            _assemblies.Add(++_counter, assembly);
           
        }

        public Assembly[] GetAssemblies()
        {
            return _assemblies.Select(x=>x.Value).ToArray();
        }

        public Assembly GetAssembly(AssemblyInProject assemblyInProject)
        {
            var key = (int)assemblyInProject;
            if (!_assemblies.ContainsKey(key))
                throw new KeyNotFoundException($"AssemblyCollector not contains key: {assemblyInProject}");

            return _assemblies[key];
        }
    }
}