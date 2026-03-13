using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AcTools.Utils.Helpers;
using JetBrains.Annotations;
using WinSCP;

namespace AcManager.Tools.Helpers.Ftp
{
	public class WinScpClient : FtpClient
	{
		private readonly SessionOptions _sessionOptions;
		
		public WinScpClient([NotNull] string hostname, [CanBeNull] string username, [CanBeNull] string password)
		{
			_sessionOptions = new SessionOptions();

			_sessionOptions.ParseUrl(hostname);
			
			if (!string.IsNullOrEmpty(username))
			{
				_sessionOptions.UserName = username;
			}
			
			if (!string.IsNullOrEmpty(password))
			{
				_sessionOptions.Password = password;
			}

			_sessionOptions.SshHostKeyPolicy = SshHostKeyPolicy.AcceptNew;
		}

		public override async Task DownloadAsync(string remoteFile, string localFile, CancellationToken cancellation = default)
		{
			using (Session session = new Session())
			{
				await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					var stream = session.GetFile(remoteFile);
					using (FileStream file = new FileStream(localFile, FileMode.Create))
					{
						stream.CopyTo(file);
					}
					
				}, cancellation);
			}
		}

		public override async Task UploadAsync(string remoteFile, string localFile, CancellationToken cancellationToken = default)
		{
			using (FileStream file = new FileStream(localFile, FileMode.Open))
			using (Session session = new Session())
			{
				await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					session.PutFile(file, remoteFile);
				}, cancellationToken);
			}
		}
		
		public override async Task DeleteAsync(string remoteFile, CancellationToken cancellationToken = default)
		{
			using (Session session = new Session())
			{
				await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					session.RemoveFile(remoteFile);
				}, cancellationToken);
			}
		}

		public override async Task RenameAsync(string remoteFile, string newFileName, CancellationToken cancellation = default)
		{
			using (Session session = new Session())
			{
				await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					session.MoveFile(remoteFile, newFileName);
				}, cancellation);
			}
		}

		public override async Task DeleteDirectoryAsync(string remoteDirectory, CancellationToken cancellationToken = default)
		{
			using (Session session = new Session())
			{
				await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					session.RemoveFiles(remoteDirectory);
				}, cancellationToken);
			}
		}
		
		public override async Task CreateDirectoryAsync(string remoteDirectory, CancellationToken cancellationToken = default)
		{
			using (Session session = new Session())
			{
				await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					session.CreateDirectory(remoteDirectory);
				}, cancellationToken);
			}
		}

		public override async Task CleanDirectoryAsync(string directory, CancellationToken cancellation = default)
		{
			var files = await DirectoryListDetailedAsync(directory, cancellation).ConfigureAwait(false);
			foreach (DetailedInformation file in files)
			{
				var filePath = $@"{directory}/{file.FileName}";
				if (file.IsDirectory)
				{
					await CleanDirectoryAsync(filePath, cancellation).ConfigureAwait(false);
					await DeleteDirectoryAsync(filePath, cancellation).ConfigureAwait(false);
				}
				else
				{
					await DeleteAsync(filePath, cancellation).ConfigureAwait(false);
				}
			}
		}

		public override async Task<string> GetFileCreatedDateTimeAsync(string remoteFile, CancellationToken cancellation = default)
		{
			using (Session session = new Session())
			{
				return await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					return session.GetFileInfo(remoteFile).LastWriteTime.ToString(CultureInfo.CurrentCulture);
				}, cancellation);
			}
		}

		public override async Task<string> GetFileSizeAsync(string remoteFile, CancellationToken cancellation = default)
		{
			using (Session session = new Session())
			{
				return await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);
					return session.GetFileInfo(remoteFile).Length.ToString();
				}, cancellation);
			}
		}

		public override async Task<string[]> DirectoryListSimpleAsync(string remoteFile, CancellationToken cancellation = default)
		{
			// Return list of file names in the directory
			using (Session session = new Session())
			{
				return await Task.Run(() =>
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);

					if (!session.FileExists(remoteFile))
					{
						return new string[0];
					}

					var file = session.GetFileInfo(remoteFile);

					if (file.IsDirectory)
					{
						var directory = session.ListDirectory(remoteFile);
						var fileNames = new string[directory.Files.Count];

						foreach (RemoteFileInfo fileInfo in directory.Files)
						{
							fileNames[directory.Files.IndexOf(fileInfo)] = fileInfo.Name;
						}

						return fileNames;
					}

					return new[] { file.Name };
				}, cancellation);
			}
		}

		internal override async Task<DetailedInformation[]> DirectoryListDetailedAsync(string remoteFile, CancellationToken cancellationToken = default)
		{
			return await Task.Run(() =>
			{
				using (Session session = new Session())
				{
					session.DisableVersionCheck = true;
					session.Open(_sessionOptions);

					if (!session.FileExists(remoteFile))
					{
						return new DetailedInformation[0];
					}

					var file = session.GetFileInfo(remoteFile);

					if (file.IsDirectory)
					{
						var directory = session.ListDirectory(remoteFile);
						var detailedInformation = new DetailedInformation[directory.Files.Count];

						foreach (RemoteFileInfo fileInfo in directory.Files)
						{
							detailedInformation[directory.Files.IndexOf(fileInfo)] = new DetailedInformation(
								fileInfo.IsDirectory,
								fileInfo.Length,
								fileInfo.LastWriteTime,
								fileInfo.Name
							);
						}

						return detailedInformation;
					}

					return new[]
					{
						new DetailedInformation(
							file.IsDirectory,
							file.Length,
							file.LastWriteTime,
							file.Name
						)
					};
				}
			}, cancellationToken);
		}
	}
}
