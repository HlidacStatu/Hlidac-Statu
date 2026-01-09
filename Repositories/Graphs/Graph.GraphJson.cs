namespace HlidacStatu.Repositories
{
    public partial class Graph
    {
        /*
  {
"data": {
"id": "e204",
"weight": 50,
"source": "n345",
"target": "n368"
},
"position": {

},
"group": "edges",
"removed": false,
"selected": false,
"selectable": true,
"locked": false,
"grabbed": false,
"grabbable": true,
"classes": ""
},
     */

        [Newtonsoft.Json.JsonObject(ItemNullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public class GraphJson
        {
            public GraphJson(HlidacStatu.DS.Graphs.Graph.Node n, int distance, bool isRoot = false)
            {
                group = "nodes";
                data.id = n.UniqId;
                data.distance = distance;
                data.caption = await n.PrintNameAsync();
                data.root = isRoot ? true : (bool?)null;
            }
            public GraphJson(HlidacStatu.DS.Graphs.Graph.Edge e)
            {
                group = "edges";

                data.caption = e.Doba();
                data.source = e.From == null ? null : e.From.UniqId;
                data.target = e.To == null ? null : e.To.UniqId;
                data.distance = e.Distance;
            }

            public Data data { get; set; } = new Data();
            public Position position { get; set; }
            public string group { get; set; }
            public bool removed { get; set; }
            public bool selected { get; set; }
            public bool selectable { get; set; } = true;
            public bool locked { get; set; }
            public bool grabbed { get; set; }
            public bool grabbable { get; set; } = true;
            public string classes { get; set; }


            [Newtonsoft.Json.JsonObject(ItemNullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
            public class Data
            {
                public string id { get; set; }

                public string caption { get; set; }
                public int weight { get; set; }
                public string source { get; set; }
                public string target { get; set; }
                public int distance { get; set; }
                public bool? root { get; set; }
            }

            public class Position
            {
                public decimal x { get; set; }
                public decimal y { get; set; }
            }

        }

    }

}
