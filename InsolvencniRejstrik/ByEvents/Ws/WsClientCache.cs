﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

		public IEnumerable<WsResult> Get(long id)
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
							fileStream.ReadLine();  // ignored because first read line could be incomplete (sought in the middle of it)
							var wsResult = WsResult.From(fileStream.ReadLine());
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
							var item = WsResult.From(fileStream.ReadLine());
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
					foreach (var item in File.ReadLines(CacheFile).SkipWhile(l => IsNotEqual(l, ids)).Select(l => WsResult.From(l)))
					{
						yield return item;
						latestId = item.Id;
					}
				}
			}

			foreach (var item in UnderlyingClient.Value.Get(latestId))
			{
				File.AppendAllLines(CacheFile, new[] { item.ToStringLine() });
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
