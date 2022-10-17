using System;
using System.IO;

namespace InsolvencniRejstrik.ByEvents
{
	class EventsRepository : IEventsRepository
	{
		private const string LastIdFile = "last_event_id.dat";
		private const long DefaultEventId = -1;

		public long GetLastEventId()
		{
			try
			{
				return File.Exists(LastIdFile) ? Convert.ToInt64(File.ReadAllText(LastIdFile)) : DefaultEventId;
			}
			catch (Exception)
			{
				return DefaultEventId;
			}
		}

		private int Count = 0;
		public void SetLastEventId(long id)
		{
			// just optimalization
			if (++Count % 1000 == 0)
			{
				File.WriteAllText(LastIdFile, id.ToString());
			}
		}
	}
}
