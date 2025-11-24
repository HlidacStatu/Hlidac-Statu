using System;

namespace HlidacStatu.CachingClients.PostgreSql
{
    public interface IDatabaseExpiredItemsRemoverLoop : IDisposable
    {
        void Start();
    }
}