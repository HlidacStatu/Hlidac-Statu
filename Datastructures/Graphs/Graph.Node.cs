using System;

namespace HlidacStatu.DS.Graphs
{
    public partial class Graph
    {
        public class Node : IComparable<Node>
        {
            public int CompareTo(Node other)
            {
                if (other == null)
                    return 1;
                if (UniqId == other.UniqId)
                    return 0;
                else
                    return -1;
            }

            public const string Prefix_NodeType_Person = "p-";
            public const string Prefix_NodeType_Company = "c-";

            public int UniqIdHashCode { get; private set; }
            string _uniqId = null;
            public string UniqId
            {
                get
                {
                    if (_uniqId == null)
                    {
                        if (Type == NodeType.Person)
                            _uniqId = Prefix_NodeType_Person + Id;
                        else
                            _uniqId = Prefix_NodeType_Company + Id;
                        UniqIdHashCode = _uniqId.GetHashCode();
                    }
                    return _uniqId;
                }
            }

            public enum NodeType { Company, Person }
            public string Id { get; set; }
            public NodeType Type { get; set; }
            public string Name { get; set; }
            public bool Highlighted { get; set; }


            public override int GetHashCode()
            {
                return HashCode.Combine(UniqId);
                //return _uniqId==null ? 0 : _uniqId.GetHashCode();
            }
        }

    }

}
