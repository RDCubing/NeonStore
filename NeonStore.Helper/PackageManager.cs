using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace NeonStore.Helper
{
    public enum SideloadAction
    {
        Install,
        Update,
        ForceUpdate
    }

    public static class PackageInstaller
    {
        public static async Task ExecuteAsync(
            string packagePath,
            SideloadAction action,
            Action<int> onProgress = null)
        {
            if (!File.Exists(packagePath))
                throw new FileNotFoundException("AppX file not found.", packagePath);

            Uri packageUri = new Uri(packagePath);
            PackageManager packageManager = new PackageManager();
            IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOp;

            switch (action)
            {
                case SideloadAction.ForceUpdate:
                    // Shuts down target app if currently running
                    deploymentOp = packageManager.UpdatePackageAsync(
                        packageUri,
                        null,
                        DeploymentOptions.ForceApplicationShutdown);
                    break;

                case SideloadAction.Update:
                    // Regular update (fails if running)
                    deploymentOp = packageManager.UpdatePackageAsync(
                        packageUri,
                        null,
                        DeploymentOptions.None);
                    break;

                case SideloadAction.Install:
                default:
                    // Standard fresh install
                    deploymentOp = packageManager.AddPackageAsync(
                        packageUri,
                        null,
                        DeploymentOptions.None);
                    break;
            }

            // Real-time progress updates
            deploymentOp.Progress = (op, progress) =>
            {
                if (progress.state == DeploymentProgressState.Processing)
                {
                    onProgress?.Invoke((int)progress.percentage);
                }
            };

            await deploymentOp;

            if (deploymentOp.Status == AsyncStatus.Error)
            {
                DeploymentResult result = deploymentOp.GetResults();
                uint hresult = (uint)deploymentOp.ErrorCode.HResult;

                if (hresult == 0x80073CFB && action == SideloadAction.Install)
                {
                    throw new Exception("App is already installed. Use 'Update' or 'Force Update'.");
                }

                if (hresult == 0x80073CF3 && action == SideloadAction.Update)
                {
                    throw new Exception("App is currently running. Use 'Force Update' to terminate it and install.");
                }

                throw new Exception(string.Format("Deployment failed (0x{0:X}): {1}", hresult, result.ErrorText));
            }
        }
    }
}