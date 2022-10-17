namespace InsolvencniRejstrik.ByEvents
{
	class NoOpEventRepository : IEventsRepository
	{
		public long GetLastEventId() => 0;
		public void SetLastEventId(long id) { }
	}
}
