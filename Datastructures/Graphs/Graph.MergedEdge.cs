using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Datastructures.Graphs
{
    public partial class Graph
    {
        public class MergedEdge
        {
            public MergedEdge() { }
            public MergedEdge(Edge e)
            {
                Aktualnost = e.Aktualnost;
                Distance = e.Distance;
                From = e.From;
                To = e.To;
                Intervals.Add(new Interval()
                {
                    Descr = e.Descr,
                    RelFrom = e.RelFrom,
                    RelTo = e.RelTo
                });

            }

            public class Interval
            {
                public DateTime? RelFrom { get; set; }
                public DateTime? RelTo { get; set; }
                public string Descr { get; set; }
                public string Doba(string format = null, string betweenDatesDelimiter = null)
                {
                    if (string.IsNullOrEmpty(format))
                        format = "({0})";
                    if (string.IsNullOrEmpty(betweenDatesDelimiter))
                        betweenDatesDelimiter = " - ";
                    string datumy = string.Empty;

                    if (RelFrom.HasValue && RelTo.HasValue)
                    {
                        datumy = string.Format("{0}{2}{1}", RelFrom.Value.ToShortDateString(), RelTo.Value.ToShortDateString(), betweenDatesDelimiter);
                    }
                    else if (RelTo.HasValue)
                    {
                        datumy = string.Format("do {0}", RelTo.Value.ToShortDateString());
                    }
                    else if (RelFrom.HasValue)
                    {
                        datumy = string.Format("od {0}", RelFrom.Value.ToShortDateString());
                    }
                    if (string.IsNullOrEmpty(datumy))
                        return string.Empty;

                    return string.Format(format, datumy);
                }

            }

            public Node From { get; set; }
            public Node To { get; set; }

            public List<Interval> Intervals { get; private set; } = new List<Interval>();

            public int Distance { get; set; }
            public Relation.AktualnostType Aktualnost { get; set; }

            public string Doba(string dateIntervalDelimiter = ",", string format = null, string betweenDatesDelimiter = null)
            {
                return string.Join(dateIntervalDelimiter, Intervals.Select(m => m.Doba(format, betweenDatesDelimiter)));
            }

            public MergedEdge MergeWith(Edge e)
            {
                if (e == null)
                    return this;

                if (To?.UniqId != e.To?.UniqId)
                    throw new ArgumentException("To properties should be same with merged Edge");
                // if (From?.UniqId != e.From?.UniqId)
                //     throw new ArgumentException("To and From properties should be same with merged Edge");

                List<Interval> newInterval = new List<Interval>();
                bool mergedInterval = false;
                foreach (var interv in Intervals)
                {
                    if (mergedInterval == false
                        && Devmasters.DT.Util.IsOverlappingIntervals(interv.RelFrom, interv.RelTo, e.RelFrom, e.RelTo)
                        )
                    {
                        Interval newI = new Interval
                        {
                            RelFrom = Devmasters.DT.Util.LowerDate(interv.RelFrom, e.RelFrom),
                            RelTo = Devmasters.DT.Util.HigherDate(interv.RelTo, e.RelTo),
                            Descr = interv.Descr + ", " + e.RelFrom
                        };
                        newInterval.Add(newI);
                        mergedInterval = true;
                    }
                    else
                        newInterval.Add(interv);
                }
                if (mergedInterval == false)
                    newInterval.Add(
                        new Interval()
                        {
                            Descr = e.Descr,
                            RelFrom = e.RelFrom,
                            RelTo = e.RelTo
                        }
                        );
                Intervals = newInterval;
                return this;
            }

        }

    }

}
