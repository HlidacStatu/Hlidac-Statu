using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InsolvencniRejstrik.ByEvents
{
	class WsClientCache : IWsClient
	{

		private readonly Lazy<IWsClient> UnderlyingClient;
		private readonly string CacheFile;
		public const long SeekStepInBytes = 1_048_576; // 1MB

		public WsClientCache(Lazy<IWsClient> underlyingClient, string cacheFile)
		{
			UnderlyingClient = underlyingClient;
			CacheFile = cacheFile;
		}

		public long GetLastIdInCache()
		{
			if (File.Exists(CacheFile))
			{
				var last = WsResult.From(File.ReadLines(CacheFile).Last());
				return last.Id;
			}

			return 0;
		}

		public async IAsyncEnumerable<WsResult> GetAsync(long id)
		{
			var latestId = id;
			if (File.Exists(CacheFile))
			{
				var fileInfo = new FileInfo(CacheFile);
				var seekEnabled = true;

				using (var fileStream = File.OpenText(CacheFile))
				{
					if (fileStream.BaseStream.CanSeek)
					{
						var currentOffset = Math.Max(0, fileInfo.Length - SeekStepInBytes);
						while (true)
						{
							if (currentOffset <= 0)
							{
								fileStream.BaseStream.Seek(0, SeekOrigin.Begin);
								fileStream.DiscardBufferedData();
								break;
							}

							fileStream.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
							fileStream.DiscardBufferedData();
							await fileStream.ReadLineAsync();  // ignored because first read line could be incomplete (sought in the middle of it)
							var wsResult = WsResult.From(await fileStream.ReadLineAsync());
							if (wsResult.Id > latestId)
							{
								currentOffset = currentOffset - SeekStepInBytes;
								continue;
							}

							// we are in front of the finding event id
							break;
						}

						while (!fileStream.EndOfStream)
						{
							var item = WsResult.From(await fileStream.ReadLineAsync());
							if (item.Id < latestId)
							{
								continue;
							}
							yield return item;
							latestId = item.Id;
						}
					}
					else
					{
						seekEnabled = false;
					}
				}

				if (!seekEnabled)
				{
					var ids = id.ToString();
					await foreach (var item in File.ReadLinesAsync(CacheFile))
					{
						if(IsNotEqual(item, ids))
							continue;

						var wsresult = WsResult.From(item);
						
						yield return wsresult;
						latestId = wsresult.Id;
					}
				}
			}

			await foreach (var item in UnderlyingClient.Value.GetAsync(latestId))
			{
				await File.AppendAllLinesAsync(CacheFile, new[] { item.ToStringLine() });
				yield return item;
			}
		}

		private bool IsNotEqual(string line, string ids)
		{
			for (var i = 0; i < ids.Length; i++)
			{
				if (line[i] != ids[i])
				{
					return true;
				}
			}
			return false;
		}
	}
}
