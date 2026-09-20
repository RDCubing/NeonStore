using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace NeonStore.Helper
{
    public static class CertificateManager
    {
        public static void TrustPackageCertificate(string packagePath)
        {
            if (!File.Exists(packagePath))
                throw new FileNotFoundException("AppX file not found.", packagePath);

            X509Certificate2 cert = new X509Certificate2(packagePath);

            // In .NET 4.5.2, X509Store does not implement IDisposable
            X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadWrite);

                X509Certificate2Collection matches = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    cert.Thumbprint,
                    false
                );

                if (matches.Count == 0)
                {
                    store.Add(cert);
                    Console.WriteLine("[Cert] Trusted: " + cert.Subject);
                }
                else
                {
                    Console.WriteLine("[Cert] Already in Trusted Root store.");
                }
            }
            finally
            {
                store.Close();
            }
        }
    }
}