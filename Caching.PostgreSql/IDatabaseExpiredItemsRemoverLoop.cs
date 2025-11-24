using System;

namespace HlidacStatu.CachingClients.PostgreSql
{
    internal interface IDatabaseExpiredItemsRemoverLoop : IDisposable
    {
        void Start();
    }
}