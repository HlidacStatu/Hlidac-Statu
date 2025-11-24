using System;

namespace HlidacStatu.Caching.PostgreSql
{
    internal interface IDatabaseExpiredItemsRemoverLoop : IDisposable
    {
        void Start();
    }
}