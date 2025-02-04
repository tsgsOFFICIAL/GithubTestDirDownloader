using System;
using System.IO;
using System.Diagnostics;
using GithubTestDirDownloader;

namespace GithubTestDirDownloader
{
    internal class Updater : IDisposable
    {
        private readonly string _repositoryOwner = "tsgsOFFICIAL";
        private readonly string _repositoryName = "GithubTestDirDownloader";
        private readonly string _folderPath = "/bin/Release/net8.0-windows/publish/win-x64";
        public event EventHandler<ProgressEventArgs>? DownloadProgress;

        /// <summary>
        /// Download the update
        /// </summary>
        /// <param name="downloadDirectory">Where to download to locally</param>
        internal async void DownloadUpdate(string basePath, string downloadDirectory)
        {
            using (GitHubDirectoryDownloader downloader = new GitHubDirectoryDownloader(_repositoryOwner, _repositoryName, _folderPath, basePath))
            {
                downloader.ProgressUpdated += OnProgressChanged!;

                await downloader.DownloadDirectoryAsync(downloadDirectory);
                try
                {
                    Process.Start(Path.Combine(downloadDirectory, "CS2-AutoAccept"));
                    Environment.Exit(0);
                }
                catch (Exception)
                { }
            }
        }
        /// <summary>
        /// Raises the ProgressChanged event.
        /// </summary>
        /// <param name="e">An instance of ProgressEventArgs, can hold Progress as an int (0-100).</param>
        protected virtual void OnProgressChanged(object sender, ProgressEventArgs e)
        {
            DownloadProgress?.Invoke(this, e);
        }

        /// <summary>
        /// Disposes of the Updater instance.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}