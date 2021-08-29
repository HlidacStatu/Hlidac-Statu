using System;

namespace HlidacStatu.XLib
{
    public static class RenderTools
    {
        public static void ProgressWriter_OutputFunc_EndIn(Devmasters.Batch.ActionProgressData data)
        {
            DateTime end = data.EstimatedFinish;
            string send = "";
            if (data.EstimatedFinish > DateTime.MinValue)
            {
                TimeSpan endIn = data.EstimatedFinish - DateTime.Now;
                send = FormatAvailability(endIn, DateTimePart.Second);
            }

            Console.WriteLine(
                string.Format($"\n{data.Prefix}{DateTime.Now.ToLongTimeString()}: {data.ProcessedItems}/{data.TotalItems} {data.PercentDone}%  End in {send}")
                );
        }

        public enum DateTimePart
        {
            Year = 1,
            Month = 2,
            Day = 3,
            Hour = 4,
            Minute = 5,
            Second = 6
        }

        public static string FormatAvailability(TimeSpan ts, DateTimePart minDatePart, string numFormat = "N1")
        {

            var end = DateTime.Now;
            Devmasters.DT.DateTimeSpan dts = Devmasters.DT.DateTimeSpan.CompareDates(end - ts, end);
            string s = "";
            if (dts.Years > 0 && minDatePart > DateTimePart.Year)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Years, "{0} rok;{0} roky;{0} let");
            }
            else if (dts.Years > 0)
            {
                decimal part = dts.Years + dts.Months / 12m;
                if (part % 1 > 0)
                    s += string.Format(" {0:" + numFormat + "} let", part);
                else
                    s += Devmasters.Lang.CS.Plural.Get((int)part, " {0} rok; {0} roky; {0} let"); ;
                return s;
            }

            if (dts.Months > 0 && minDatePart > DateTimePart.Month)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Months, "{0} měsíc;{0} měsíce;{0} měsíců");
            }
            else if (dts.Months > 0)
            {
                decimal part = dts.Months + dts.Days / 30m;
                if (part % 1 > 0)
                    s += string.Format(" {0:" + numFormat + "} měsíců", part);
                else
                    s += Devmasters.Lang.CS.Plural.Get((int)part, " {0} měsíc; {0} měsíce; {0} měsíců"); ;
                return s;
            }

            if (dts.Days > 0 && minDatePart > DateTimePart.Day)
            {
                s = " " + Devmasters.Lang.CS.Plural.Get(dts.Days, " {0} den;{0} dny;{0} dnů");
            }
            else if (dts.Days > 0)
            {
                decimal part = dts.Days + dts.Hours / 24m;
                if (part % 1 > 0)
                    s = " " + string.Format(" {0:" + numFormat + "} dní", part);
                else
                    s = " " + Devmasters.Lang.CS.Plural.Get((int)part, " {0} den;{0} dny;{0} dnů"); ;
                return s;
            }

            if (dts.Hours > 0 && minDatePart > DateTimePart.Hour)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Hours, " {0} hodinu;{0} hodiny;{0} hodin");
            }
            else if (dts.Hours > 0)
            {
                decimal part = dts.Hours + dts.Minutes / 60m;
                if (part % 1 > 0)
                    s += string.Format(" {0:" + numFormat + "} hodin", part);
                else
                    s += " " + Devmasters.Lang.CS.Plural.Get((int)part, " {0} hodinu;{0} hodiny;{0} hodin");
                return s;
            }

            if (dts.Minutes > 0 && minDatePart > DateTimePart.Minute)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Minutes, "minutu;{0} minuty;{0} minut");
            }
            else if (dts.Minutes > 0)
            {
                decimal part = dts.Minutes + dts.Seconds / 60m;
                if (part % 1 > 0)
                    s += string.Format(" {0:" + numFormat + "} minut", part);
                else
                    s += " " + Devmasters.Lang.CS.Plural.Get((int)part, "minutu;{0} minuty;{0} minut"); ;
                return s;
            }

            if (dts.Seconds > 0 && minDatePart > DateTimePart.Second)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Seconds, "sekundu;{0} sekundy;{0} sekund");
            }
            else
            {
                decimal part = dts.Seconds + dts.Milliseconds / 1000m;
                if (part % 1 > 0)
                    s += string.Format(" {0:" + numFormat + "} sekund", part);
                else
                    s += " " + Devmasters.Lang.CS.Plural.Get((int)part, "sekundu;{0} sekundy;{0} sekund"); ;
                return s;
            }

            //if (dts.Milliseconds > 0)
            //    s += " " + Devmasters.Lang.CS.Plural.Get(dts.Milliseconds, "{0} ms;{0} ms;{0} ms");

            return s.Trim();

        }

        public static string FormatAvailability2(TimeSpan ts)
        {
            var end = DateTime.Now;
            Devmasters.DT.DateTimeSpan dts = Devmasters.DT.DateTimeSpan.CompareDates(end - ts, end);
            string s = "";
            if (dts.Years > 0)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Years, "rok;{0} roky;{0} let");
            }
            if (dts.Months > 0)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Months, "měsíc;{0} měsíce;{0} měsíců");
            }
            if (dts.Days > 0)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Days, "den;{0} dny;{0} dnů");
            }
            if (dts.Hours > 0)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Hours, "hodinu;{0} hodiny;{0} hodin");
            }
            if (dts.Minutes > 0)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Minutes, "minutu;{0} minuty;{0} minut");
            }
            if (dts.Seconds > 0)
            {
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Seconds, "sekundu;{0} sekundy;{0} sekund");
            }
            if (dts.Milliseconds > 0)
                s += " " + Devmasters.Lang.CS.Plural.Get(dts.Milliseconds, "{0} ms;{0} ms;{0} ms");

            return s.Trim();

        }

        public static string DateDiffShort(DateTime first, DateTime sec, string beforeTemplate, string afterTemplate)
        {
            if (first < DateTime.MinValue)
                first = DateTime.MinValue;
            if (sec < DateTime.MinValue)
                sec = DateTime.MinValue;
            if (first > DateTime.MaxValue)
                first = DateTime.MaxValue;
            if (sec > DateTime.MaxValue)
                sec = DateTime.MaxValue;

            bool after = first > sec;
            Devmasters.DT.DateTimeSpan dateDiff = Devmasters.DT.DateTimeSpan.CompareDates(first, sec);
            string txtDiff = string.Empty;
            if (dateDiff.Years > 0)
            {
                txtDiff = Devmasters.Lang.CS.Plural.Get(dateDiff.Years, "rok;{0} roky;{0} let");
            }
            else if (dateDiff.Months > 3)
            {
                txtDiff = Devmasters.Lang.CS.Plural.Get(dateDiff.Months, "měsíc;{0} měsíce;{0} měsíců");
            }
            else
            {
                txtDiff = Devmasters.Lang.CS.Plural.GetWithZero(dateDiff.Days, "dnes", "den", "{0} dny", "{0} dnů");
            }

            if (after)
                return string.Format(afterTemplate, txtDiff);
            else
                return string.Format(beforeTemplate, txtDiff);

        }


        public static string DateDiffShort_7pad(DateTime first, DateTime sec, string beforeTemplate, string afterTemplate)
        {
            if (first < DateTime.MinValue)
                first = DateTime.MinValue;
            if (sec < DateTime.MinValue)
                sec = DateTime.MinValue;
            if (first > DateTime.MaxValue)
                first = DateTime.MaxValue;
            if (sec > DateTime.MaxValue)
                sec = DateTime.MaxValue;

            bool after = first > sec;
            Devmasters.DT.DateTimeSpan dateDiff = Devmasters.DT.DateTimeSpan.CompareDates(first, sec);
            if (after)
            {
                string txtDiff = string.Empty;
                if (dateDiff.Years > 0)
                {
                    txtDiff = Devmasters.Lang.CS.Plural.Get(dateDiff.Years, "roce;{0} letech;{0} letech");
                }
                else if (dateDiff.Months > 3)
                {
                    txtDiff = Devmasters.Lang.CS.Plural.Get(dateDiff.Months, "měsíci;{0} měsících;{0} měsících");
                }
                else
                {
                    txtDiff = Devmasters.Lang.CS.Plural.GetWithZero(dateDiff.Days, "pár hodinách", "jednom dni", "{0} dnech", "{0} dny", "{0} dnů");
                }

                return string.Format(afterTemplate, txtDiff);
            }
            else
            {
                string txtDiff = string.Empty;
                if (dateDiff.Years > 0)
                {
                    txtDiff = Devmasters.Lang.CS.Plural.Get(dateDiff.Years, "rokem;{0} roky;{0} roky");
                }
                else if (dateDiff.Months > 3)
                {
                    txtDiff = Devmasters.Lang.CS.Plural.Get(dateDiff.Months, "měsícem;{0} měsíci;{0} měsíci");
                }
                else
                {
                    txtDiff = Devmasters.Lang.CS.Plural.GetWithZero(dateDiff.Days, "pár hodinami", "dnem", "{0} dny", "{0} dny");
                }

                return string.Format(beforeTemplate, txtDiff);
            }

        }
    }
}
