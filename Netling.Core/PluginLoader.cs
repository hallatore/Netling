using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netling.Core
{
    public class PluginInfo
    {
        public string AssemblyShort { get; set; }
        public string AssemblyName { get; set; }
        public string TypeName { get; set; }
    }

    public class PluginLoader
    {
        public static List<PluginInfo>  GetPluginList(string relativePath)
        {
            List<PluginInfo> plugins = new List<PluginInfo>();

            string root = Path.GetFullPath(Path.GetDirectoryName(Path.Combine(typeof(PluginLoader).Assembly.Location)));
            string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));

            DirectoryInfo dir = new DirectoryInfo(pluginLocation);

            foreach (FileInfo file in dir.GetFiles("*.dll"))
            {
                Assembly assembly = Assembly.LoadFrom(file.FullName);
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IWorkerJob).IsAssignableFrom(type) && type.IsAbstract == false)
                    {
                        PluginInfo info = new PluginInfo
                        {
                            AssemblyShort = file.Name,
                            AssemblyName = file.FullName,
                            TypeName = type.Name,
                        };

                        plugins.Add(info);
                    }
                }
            }

            return plugins;
        }

        public static Assembly LoadPlugin(string pluginLocation)
        {
            Console.WriteLine($"Loading commands from: {pluginLocation}");

            Assembly assembly = Assembly.LoadFrom(pluginLocation);

            return assembly;
        }

        public static IWorkerJob CreateInstance(Assembly assembly, string typeName)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IWorkerJob).IsAssignableFrom(type) && type.Name == typeName)
                {
                    IWorkerJob result = Activator.CreateInstance(type) as IWorkerJob;

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));

            throw new ApplicationException(
                $"Can't find any type which implements IWorkerJob in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }

        public static IEnumerable<IWorkerJob> CreateInstances(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IWorkerJob).IsAssignableFrom(type))
                {
                    IWorkerJob result = Activator.CreateInstance(type) as IWorkerJob;

                    if (result != null)
                    {
                        count++;
                        yield return result;
                    }
                }
            }

            if (count == 0)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));

                throw new ApplicationException(
                    $"Can't find any type which implements IWorkerJob in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }
        }
    }
}
