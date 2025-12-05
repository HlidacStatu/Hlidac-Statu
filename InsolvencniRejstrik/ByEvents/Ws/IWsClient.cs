using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsolvencniRejstrik.ByEvents
{
	interface IWsClient
	{
		IAsyncEnumerable<WsResult> GetAsync(long id);
	}
}
