using System;
using System.IO;
using System.Reflection;

namespace Ryujinx.Common
{
    public static class Constants
    {
        public static string Version { get; }
        public static string BasePath { get; }

        static Constants()
        {
            Version  = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx");
        }
    }
}