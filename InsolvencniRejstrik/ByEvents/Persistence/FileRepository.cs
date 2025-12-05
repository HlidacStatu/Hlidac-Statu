using HlidacStatu.Entities.Insolvence;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InsolvencniRejstrik.ByEvents
{
	public class FileRepository : IRepository
	{
		public async Task<Rizeni> GetInsolvencyProceedingAsync(string spisovaZnacka,
			Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			var noveRizeni = createNewInsolvencyProceeding(spisovaZnacka);
			var filePath = GetFilePath(noveRizeni);

			return File.Exists(filePath.FullPath)
				? JsonConvert.DeserializeObject<Rizeni>(await File.ReadAllTextAsync(filePath.FullPath))
				: noveRizeni;
		}

		public async Task SetInsolvencyProceedingAsync(Rizeni item)
		{
			var filePath = GetFilePath(item);

			try
			{
				await File.WriteAllTextAsync(filePath.FullPath, JsonConvert.SerializeObject(item, Formatting.Indented));
			}
			catch (DirectoryNotFoundException)
			{
				Directory.CreateDirectory(filePath.Dir);
				await File.WriteAllTextAsync(filePath.FullPath, JsonConvert.SerializeObject(item, Formatting.Indented));
			}
		}

		private FilePath GetFilePath(Rizeni item)
		{
			var dir = $@"data\{item.SpisovaZnacka.Split('/')[1]}";
			return new FilePath { FullPath = $@"{dir}\{item.NormalizedId()}.json", Dir = dir };
		}

		private class FilePath
		{
			public string Dir { get; set; }
			public string FullPath { get; set; }
		}
	}
}
