using Devmasters.Enums;

using System.ComponentModel.DataAnnotations.Schema;


namespace HlidacStatu.Entities
{
    public partial class WatchDog
    {
        public static string APIID_Prefix = "APIID:";

        public static string AllDbDataType = "#ALL#";

        [ShowNiceDisplayName()]
        [Sortable(SortableAttribute.SortAlgorithm.BySortValue)]
        public enum PeriodTime
        {
            [Disabled()]
            [SortValue(0), NiceDisplayName("Ihned")]
            Immediatelly = 0,

            [Disabled()]
            [SortValue(0), NiceDisplayName("Každé 2 hodiny")]
            Hourly = 1,

            [SortValue(0), NiceDisplayName("Denně")]
            Daily = 2,

            [SortValue(0), NiceDisplayName("Týdně")]
            Weekly = 3,

            [SortValue(0), NiceDisplayName("Měsíčně")]
            Monthly = 4,
        }


        public enum FocusType
        {
            Search = 0,
            Issues = 1
        }

        [NotMapped]
        public FocusType Focus
        {
            get { return (FocusType)FocusId; }
            set { FocusId = (int)value; }
        }

        [NotMapped]
        public PeriodTime Period
        {
            get { return (PeriodTime)PeriodId; }
            set { PeriodId = (int)value; }
        }



        public enum DisabledBySystemReasons
        {
            NoDataset,
            NoConfirmedEmail,
            InvalidQuery
        }




    }
}
