using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsolvencniRejstrik.ByEvents
{
	class WsClient : IWsClient
	{
		private IsirWs.IsirWsPublicPortTypeClient Client = new IsirWs.IsirWsPublicPortTypeClient();
		
		public async IAsyncEnumerable<WsResult> GetAsync(long id)
		{
			var latestId = id;
			IsirWs.getIsirWsPublicPodnetIdResponse response;
			do
			{
				response = await Client
					.getIsirWsPublicPodnetIdAsync(new IsirWs.getIsirWsPublicPodnetIdRequest { idPodnetu = latestId });
				if (response.status.stav == IsirWs.stavType.OK)
				{
					foreach (var item in response.data)
					{
						yield return WsResult.From(item);
						latestId = item.id;
					}
				}
				else
				{
					throw new WsClientException($"WS client returns {response.status.stav} ({response.status.kodChyby})\n {response.status.popisChyby}");
				}
			} while (response.status.stav == IsirWs.stavType.OK && response.data.Count() > 0);
		}
	}

	class WsClientException : ApplicationException
	{
		public WsClientException(string message)
			: base(message)
		{ }
	}
}
