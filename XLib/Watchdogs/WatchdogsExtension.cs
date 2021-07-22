using System.Collections.Generic;
using HlidacStatu.Datasets;
using HlidacStatu.Entities;

namespace HlidacStatu.XLib.Watchdogs
{
    public static class WatchdogsExtension
    {
        public static List<IWatchdogProcessor> GetWatchDogProcessors(this WatchDog watchDog)
        {

            var res = new List<IWatchdogProcessor>();


            if (watchDog.DataType == nameof(Smlouva) || watchDog.DataType == WatchDog.AllDbDataType)
                res.Add(new SmlouvaWatchdogProcessor(watchDog));
            if (watchDog.DataType == nameof(Entities.VZ.VerejnaZakazka) || watchDog.DataType == WatchDog.AllDbDataType)
                res.Add(new VerejnaZakazkaWatchdogProcessor(watchDog));
            if (watchDog.DataType == nameof(Entities.Insolvence.Rizeni) || watchDog.DataType == WatchDog.AllDbDataType)
                res.Add(new RizeniWatchdogProcessor(watchDog));
            if (watchDog.DataType.StartsWith(nameof(DataSet)))
                res.Add(new DataSetWatchdogProcessor(watchDog));

            if (watchDog.DataType == WatchDog.AllDbDataType)
            {                                                     //add all datasets
                foreach (var ds in DataSetDB.ProductionDataSets.Get())
                {
                    res.Add(new DataSetWatchdogProcessor(watchDog, ds));
                }

            }

            return res;
        }
    }
}