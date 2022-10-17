using HlidacStatu.Entities.Insolvence;
using Newtonsoft.Json;
using System;
using System.IO;

namespace InsolvencniRejstrik.ByEvents
{
	public class FileRepository : IRepository
	{
		public Rizeni GetInsolvencyProceeding(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			var noveRizeni = createNewInsolvencyProceeding(spisovaZnacka);
			var filePath = GetFilePath(noveRizeni);

			return File.Exists(filePath.FullPath)
				? JsonConvert.DeserializeObject<Rizeni>(File.ReadAllText(filePath.FullPath))
				: noveRizeni;
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			var filePath = GetFilePath(item);

			try
			{
				File.WriteAllText(filePath.FullPath, JsonConvert.SerializeObject(item, Formatting.Indented));
			}
			catch (DirectoryNotFoundException)
			{
				Directory.CreateDirectory(filePath.Dir);
				File.WriteAllText(filePath.FullPath, JsonConvert.SerializeObject(item, Formatting.Indented));
			}
		}

		private FilePath GetFilePath(Rizeni item)
		{
			var dir = $@"data\{item.SpisovaZnacka.Split('/')[1]}";
			return new FilePath { FullPath = $@"{dir}\{item.UrlId()}.json", Dir = dir };
		}

		private class FilePath
		{
			public string Dir { get; set; }
			public string FullPath { get; set; }
		}
	}
}
