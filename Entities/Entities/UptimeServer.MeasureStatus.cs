using System;

namespace HlidacStatu.Entities
{
    public partial class UptimeServer
    {
        public class MeasureStatus<T>
        {
            public MeasureStatus() { }
            public MeasureStatus(T ok, T pomale, T nedostupne, T unknown = default(T))
            {
                OK = ok;
                Pomale = pomale;
                Nedostupne = nedostupne;
                Unknown = unknown;
            }

            public T OK { get; set; } = default(T);
            public T Pomale { get; set; } = default(T);
            public T Nedostupne { get; set; } = default(T);
            public T Unknown { get; set; } = default(T);

            public T Get(UptimeSSL.Statuses status)
            {
                switch (status)
                {
                    case UptimeSSL.Statuses.OK:
                        return OK;
                    case UptimeSSL.Statuses.Pomalé:
                        return Pomale;
                    case UptimeSSL.Statuses.Nedostupné:
                        return Nedostupne;
                    case UptimeSSL.Statuses.Unknown:
                        return Unknown;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

    }
}
