using System.Collections.Generic;

namespace HlidacStatu.DS.Graphs
{
    public partial class Graph
    {
        public class NodeComparer : IEqualityComparer<Node>
        {
            public bool Equals(Node x, Node y)
            {
                //Check whether the compared objects reference the same data.
                if (ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null.
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;

                return x.UniqId == y.UniqId;
            }

            public int GetHashCode(Node obj)
            {
                //Check whether the object is null
                if (ReferenceEquals(obj, null)) return 0;

                //Get hash code for the Name field if it is not null.
                return obj.UniqId == null ? 0 : obj.UniqIdHashCode;

            }
        }

    }

}
