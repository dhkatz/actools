using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace AcManager.Tools.Helpers.Ftp
{
	public abstract class FtpClient
	{
		public abstract Task DownloadAsync([NotNull] string remoteFile, [NotNull] string localFile, CancellationToken cancellation = default);
		public abstract Task UploadAsync([NotNull] string remoteFile, [NotNull] string localFile, CancellationToken cancellation = default);
		public abstract Task DeleteAsync([NotNull] string remoteFile, CancellationToken cancellation = default);
		public abstract Task RenameAsync([NotNull] string remoteFile, [NotNull] string newFileName, CancellationToken cancellation = default);
		public abstract Task DeleteDirectoryAsync([NotNull] string newDirectory, CancellationToken cancellation = default);
		public abstract Task CreateDirectoryAsync([NotNull] string newDirectory, CancellationToken cancellation = default);
		public abstract Task CleanDirectoryAsync([NotNull] string directory, CancellationToken cancellation = default);
		public abstract Task<string> GetFileCreatedDateTimeAsync([NotNull] string remoteFile, CancellationToken cancellation = default);
		public abstract Task<string> GetFileSizeAsync([NotNull] string remoteFile, CancellationToken cancellation = default);
		public abstract Task<string[]> DirectoryListSimpleAsync([NotNull] string remoteFile, CancellationToken cancellation = default);

		internal abstract Task<DetailedInformation[]> DirectoryListDetailedAsync([NotNull] string remoteFile, CancellationToken cancellation = default);
	}
}
