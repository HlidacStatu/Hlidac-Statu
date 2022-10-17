using System.Collections.Generic;

namespace InsolvencniRejstrik.ByEvents
{
	interface IWsClient
	{
		IEnumerable<WsResult> Get(long id);
	}
}
