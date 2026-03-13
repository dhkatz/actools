using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FirstFloor.ModernUI.Serialization;

namespace AcManager.Tools.Helpers.Ftp
{
	internal class DetailedInformation
	{
		public bool IsDirectory { get; }
		public long Size { get; }
		public DateTime LastWriteTime { get; }
		public string FileName { get; }

		internal DetailedInformation(bool isDirectory, long size, DateTime lastWriteTime, string fileName)
		{
			IsDirectory = isDirectory;
			Size = size;
			LastWriteTime = lastWriteTime;
			FileName = fileName;
		}

		public static DetailedInformation[] Create(string data)
		{
			return data.TrimEnd().Split('\n').Select(x => new
			{
				Type = x.Length < 1 ? '\0' : x[0],
				Match = Regex.Match(x, @"\s(\d+) ((?:Feb|Ma[ry]|A(?:pr|ug)|J(?:an|u[ln])|Sep|Oct|Nov|Dec) (?: \d|\d[\d ])) ( \d{4}|\d\d:\d\d) (.+?)(?:->.+)?$",
					RegexOptions.IgnoreCase)
			}).Select(x =>
			{
				var fileName = x.Match.Groups[4].Value.Trim();
				return new DetailedInformation(IsDirectory(x.Type, fileName), x.Match.Groups[1].As<long>(), GetDate(x.Match.Groups), fileName);
			}).ToArray();

			bool IsDirectory(char type, string fileName)
			{
				return type == 'd' || type == 'l' && fileName.LastIndexOf('.') < fileName.Length - 4;
			}

			DateTime GetDate(GroupCollection p)
			{
				var year = p[3].Length == 4 ? p[3].As<int>() : DateTime.Now.Year;
				var monthAndDay = p[2];
				var time = p[3].Length == 4 ? @"00:00" : p[3].Value;
				return DateTime.Parse($@"{year} {monthAndDay} {time}", CultureInfo.InvariantCulture);
			}
		}
	}
}
