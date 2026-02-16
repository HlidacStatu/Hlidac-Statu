using System;
using System.Collections.Generic;

namespace HlidacStatu.DS.Graphs2
{
    public interface IVertex
    {
        string Id { get; }
        HashSet<IEdge> OutgoingEdges { get; }

        void AddOutgoingEdge(IEdge edge);
    }

    public class Vertex<T> : IEquatable<Vertex<T>>, IVertex where T : IEquatable<T>
    {
        public string Id { get; }
        public T Value { get; }
        public HashSet<IEdge> OutgoingEdges { get; } = new HashSet<IEdge>();

        public Vertex(string id, T value)
        {
            Id = id;
            Value = value;
        }

        object lockObj = new object();
        public void AddOutgoingEdge(IEdge edge)
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

        public bool Equals(Vertex<T> other)
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
