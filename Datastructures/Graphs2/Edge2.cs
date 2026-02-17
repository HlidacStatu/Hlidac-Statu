using Devmasters.DT;
using System;

namespace HlidacStatu.DS.Graphs2
{
    public interface IEdge2
    {
        IVertex2 From { get; }
        IVertex2 To { get; }
        Devmasters.DT.DateInterval ConnectionInterval { get; }
    }

    public class HSEdge2 : Edge2<Graphs.Graph.Edge>
    {
        public HSEdge2(IVertex2 from, IVertex2 to, DateInterval connectionInterval, Graphs.Graph.Edge bindingPayload) : base(from, to, connectionInterval, bindingPayload)
        {
        }
    }
    public class Edge2<T> : IEdge2, IEquatable<Edge2<T>>
    {
        public T BindingPayload { get; }
        public IVertex2 From { get; }
        public IVertex2 To { get; }
        public Devmasters.DT.DateInterval ConnectionInterval { get; }


        public Edge2(IVertex2 from, IVertex2 to, DateInterval connectionInterval, T bindingPayload)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            BindingPayload = bindingPayload;
            this.ConnectionInterval = connectionInterval ?? new DateInterval((DateTime?)null, (DateTime?)null);
        }

        public bool Equals(Edge2<T> other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + From.GetHashCode();
                hash = hash * 23 + To.GetHashCode();
                return hash;
            }
        }

    }
}
