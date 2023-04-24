using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Tools.Helpers.Ftp {
    // Based on https://www.codeproject.com/Tips/443588/Simple-Csharp-FTP-Class, but changed quite a bit
    public class BasicFtpClient : FtpClient
    {
        [NotNull]
        private readonly string _hostName;

        [CanBeNull]
        private readonly string _userName;

        [CanBeNull]
        private readonly string _password;

        public int BufferSize { get; set; } = 8192;
        public bool UseBinary { get; set; } = true;
        public bool UsePassive { get; set; } = true;
        public bool KeepAlive { get; set; } = true;
        public TimeSpan Timeout { get; set; } = TimeSpan.MaxValue;

        public BasicFtpClient([NotNull] string hostName, [CanBeNull] string userNameName, [CanBeNull] string password) {
            _hostName = hostName;
            _userName = userNameName;
            _password = password;
        }

        private KillerOrder<FtpWebRequest> Request([NotNull] string remoteFile, [NotNull] string method, CancellationToken cancellation) {
            cancellation.ThrowIfCancellationRequested();
            var killer = KillerOrder.Create((FtpWebRequest)WebRequest.Create($"{_hostName}/{remoteFile}"), Timeout, cancellation);
            var request = killer.Victim;
            request.Credentials = new NetworkCredential(_userName, _password);
            request.UseBinary = UseBinary;
            request.UsePassive = UsePassive;
            request.KeepAlive = KeepAlive;
            request.Method = method;
            return killer;
        }

        public override async Task DownloadAsync([NotNull] string remoteFile, [NotNull] string localFile, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.DownloadFile, cancellation))
            using (var response = (FtpWebResponse)await killer.Victim.GetResponseAsync().ConfigureAwait(false))
            using (var stream = response.GetResponseStream()) {
                cancellation.ThrowIfCancellationRequested();
                if (stream == null) throw new Exception("No response");
                using (var local = new FileStream(localFile, FileMode.Create)) {
                    await stream.CopyToAsync(local, BufferSize, progress: killer.DelayingProgress<long>()).ConfigureAwait(false);
                }
            }
        }

        public override async Task UploadAsync(string remoteFile, string localFile, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.UploadFile, cancellation))
            using (var local = new FileStream(localFile, FileMode.Open))
            using (var stream = await killer.Victim.GetRequestStreamAsync()) {
                cancellation.ThrowIfCancellationRequested();
                killer.Pause();
                await Task.Run(() => local.CopyTo(stream, BufferSize)).ConfigureAwait(false);
            }
        }

        public override async Task DeleteAsync(string remoteFile, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.DeleteFile, cancellation)) {
                await killer.Victim.GetResponseAsync().ConfigureAwait(false);
            }
        }

        public override async Task RenameAsync(string remoteFile, string newFileName, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.Rename, cancellation)) {
                killer.Victim.RenameTo = newFileName;
                await killer.Victim.GetResponseAsync().ConfigureAwait(false);
            }
        }

        public override async Task DeleteDirectoryAsync( string newDirectory, CancellationToken cancellation = default) {
            using (var killer = Request(newDirectory, WebRequestMethods.Ftp.RemoveDirectory, cancellation)) {
                await killer.Victim.GetResponseAsync().ConfigureAwait(false);
            }
        }

        public override async Task CreateDirectoryAsync( string newDirectory, CancellationToken cancellation = default) {
            using (var killer = Request(newDirectory, WebRequestMethods.Ftp.MakeDirectory, cancellation)) {
                await killer.Victim.GetResponseAsync().ConfigureAwait(false);
            }
        }

        public override async Task CleanDirectoryAsync( string directory, CancellationToken cancellation = default){
            var files = await DirectoryListDetailedAsync(directory, cancellation).ConfigureAwait(false);
            foreach (var file in files){
                var filePath = $@"{directory}/{file.FileName}";
                if (file.IsDirectory){
                    await CleanDirectoryAsync(filePath, cancellation).ConfigureAwait(false);
                    await DeleteDirectoryAsync(filePath, cancellation).ConfigureAwait(false);
                } else {
                    await DeleteAsync(filePath, cancellation).ConfigureAwait(false);
                }
            }
        }

        public override async Task<string> GetFileCreatedDateTimeAsync( string remoteFile, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.GetDateTimestamp, cancellation))
            using (var response = (FtpWebResponse)await killer.Victim.GetResponseAsync().ConfigureAwait(false))
            using (var stream = response.GetResponseStream()) {
                cancellation.ThrowIfCancellationRequested();
                if (stream == null) throw new Exception("No response");
                return (await stream.ReadAsBytesAsync()).ToUtf8String();
            }
        }

        public override async Task<string> GetFileSizeAsync( string remoteFile, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.GetFileSize, cancellation))
            using (var response = (FtpWebResponse)await killer.Victim.GetResponseAsync().ConfigureAwait(false))
            using (var stream = response.GetResponseStream()) {
                cancellation.ThrowIfCancellationRequested();
                if (stream == null) throw new Exception("No response");
                return (await stream.ReadAsBytesAsync()).ToUtf8String();
            }
        }

        public override async Task<string[]> DirectoryListSimpleAsync( string remoteFile, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.ListDirectory, cancellation))
            using (var response = (FtpWebResponse)await killer.Victim.GetResponseAsync().ConfigureAwait(false))
            using (var stream = response.GetResponseStream()) {
                cancellation.ThrowIfCancellationRequested();
                if (stream == null) throw new Exception("No response");
                return (await stream.ReadAsBytesAsync()).ToUtf8String().TrimEnd().Split('\n');
            }
        }

        internal override async Task<DetailedInformation[]> DirectoryListDetailedAsync( string remoteFile, CancellationToken cancellation = default) {
            using (var killer = Request(remoteFile, WebRequestMethods.Ftp.ListDirectoryDetails, cancellation))
            using (var response = (FtpWebResponse)await killer.Victim.GetResponseAsync().ConfigureAwait(false))
            using (var stream = response.GetResponseStream()) {
                cancellation.ThrowIfCancellationRequested();
                if (stream == null) throw new Exception("No response");
                return DetailedInformation.Create(stream.ReadAsString());
            }
        }
    }
}