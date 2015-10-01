using Microsoft.Win32;

namespace RawInputProcessor.Win32
{
    internal static class RegistryAccess
    {
        private const string Prefix = @"\\?\";

        internal static RegistryKey GetDeviceKey(string device)
        {
            if (device == null || !device.StartsWith(Prefix)) return null;
            string[] array = device.Substring(Prefix.Length).Split('#');
            if (array.Length < 3) return null;
            return Registry.LocalMachine.OpenSubKey(string.Format(@"System\CurrentControlSet\Enum\{0}\{1}\{2}", 
                array[0], array[1], array[2]));
        }

        internal static string GetClassType(string classGuid)
        {
            RegistryKey registryKey =
                Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\" + classGuid);
            if (registryKey == null)
            {
                return string.Empty;
            }
            return (string) registryKey.GetValue("Class");
        }
    }
}