using System;
using System.Reflection;
using Microsoft.Win32;

namespace NeonStore.Helper
{
    public static class ProtocolRegistrar
    {
        private const string Scheme = "neonstore-helper";

        public static void EnsureRegistered()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + Scheme))
                {
                    key.SetValue("", "URL:NeonStore Helper Protocol");
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey.SetValue("", string.Format("\"{0}\" \"%1\"", exePath));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to register protocol: " + ex.Message);
            }
        }
    }
}