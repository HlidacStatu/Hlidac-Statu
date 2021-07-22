using System;

namespace HlidacStatu.Lib.Data.External.Zabbix
{
    
    public class ZabDataPerStatus<T>
    {
        public ZabDataPerStatus() { }
        public ZabDataPerStatus(T ok, T pomale, T nedostupne, T unknown = default(T))
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

        public T Get(Statuses status)
        {
            switch (status)
            {
                case Statuses.OK:
                    return OK;
                case Statuses.Pomalé:
                    return Pomale;
                case Statuses.Nedostupné:
                    return Nedostupne;
                case Statuses.Unknown:
                    return Unknown;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}