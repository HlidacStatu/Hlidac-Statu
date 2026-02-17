using System;
using System.Collections.Generic;

namespace HlidacStatu.DS.Graphs2
{
    public interface IVertex2
    {
        string Id { get; }
        HashSet<IEdge2> OutgoingEdges { get; }

        void AddOutgoingEdge(IEdge2 edge);
    }

    public class Vertex2<T> : IEquatable<Vertex2<T>>, IVertex2 where T : IEquatable<T>
    {
        public string Id { get; }
        public T Value { get; }
        public HashSet<IEdge2> OutgoingEdges { get; } = new HashSet<IEdge2>();

        public Vertex2(string id, T value)
        {
            Id = id;
            Value = value;
        }

        object lockObj = new object();
        public void AddOutgoingEdge(IEdge2 edge)
        {
            lock (lockObj)
            {
                try
                {
                    OutgoingEdges.Add(edge);
                }
                catch (Exception e)
                {

                    throw;
                }

            }
        }

        public bool Equals(Vertex2<T> other)
        {
            return this.GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
