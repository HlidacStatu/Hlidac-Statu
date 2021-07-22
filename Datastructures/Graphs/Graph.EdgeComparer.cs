using System.Collections.Generic;

namespace HlidacStatu.Datastructures.Graphs
{
    public partial class Graph
    {
        public class EdgeComparer : IEqualityComparer<Edge>
        {
            public bool Equals(Edge x, Edge y)
            {
                //Check whether the compared objects reference the same data.
                if (ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null.
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;

                return x.GetHashCode() == y.GetHashCode();
            }

            public int GetHashCode(Edge obj)
            {
                //Check whether the object is null
                if (ReferenceEquals(obj, null)) return 0;

                //Get hash code for the Name field if it is not null.
                return obj.GetHashCode();

            }
        }

    }

}
