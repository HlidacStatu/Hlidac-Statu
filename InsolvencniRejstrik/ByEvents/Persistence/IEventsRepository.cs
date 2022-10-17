namespace InsolvencniRejstrik.ByEvents
{
	interface IEventsRepository
	{
		long GetLastEventId();
		void SetLastEventId(long id);
	}
}