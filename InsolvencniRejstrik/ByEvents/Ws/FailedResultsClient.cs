using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	class FailedResultsClient : IWsClient
	{
		public const string OriginFile = "failed_messages.dat";
		public const string ProcessingFile = "failed_messages_for_read.dat";

		public IEnumerable<WsResult> Get(long id)
		{
			if (File.Exists(OriginFile))
			{
				File.Move(OriginFile, ProcessingFile);

				foreach (var item in File.ReadLines(ProcessingFile).Select(l => WsResult.From(l)))
				{
					yield return item;
				}

				File.Delete(ProcessingFile);
			}
		}
	}
}
