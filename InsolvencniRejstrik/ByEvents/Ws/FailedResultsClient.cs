using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	class FailedResultsClient : IWsClient
	{
		public const string OriginFile = "failed_messages.dat";
		public const string ProcessingFile = "failed_messages_for_read.dat";

		public async IAsyncEnumerable<WsResult> GetAsync(long id)
		{
			if (File.Exists(OriginFile))
			{
				File.Move(OriginFile, ProcessingFile);

				await foreach (var line in File.ReadLinesAsync(ProcessingFile))
				{
					var item = WsResult.From(line);
					yield return item;
				}

				File.Delete(ProcessingFile);
			}
		}
	}
}
