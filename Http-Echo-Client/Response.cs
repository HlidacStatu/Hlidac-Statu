namespace Http_Echo
{
    public partial class Client
    {

        public class Response
        {

            public string path { get; set; }
            public Headers headers { get; set; }
            public string method { get; set; }
            public string body { get; set; }
            public bool fresh { get; set; }
            public string hostname { get; set; }
            public string ip { get; set; }
            public string[] ips { get; set; }
            public string protocol { get; set; }
            public Query query { get; set; }
            public string[] subdomains { get; set; }
            public bool xhr { get; set; }
            public Os os { get; set; }
            public Connection connection { get; set; }

            public class Headers
            {
                public string host { get; set; }
                public string useragent { get; set; }
                public string accept { get; set; }
            }

            public class Query
            {
            }

            public class Os
            {
                public string hostname { get; set; }
            }

            public class Connection
            {
            }

        }


    }
}