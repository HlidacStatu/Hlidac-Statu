using System;
using System.Data;
using Serilog;

namespace HlidacStatu.Entities
{
    public class DumpData
    {
        public enum ShouldDownloadStatus
        {
            Yes,
            No,
            WaitForData
        }
        
        private static readonly ILogger _logger = Log.ForContext<DumpData>();

        public static ShouldDownloadStatus ShouldDownload(XML.indexDump dump)
        {


            string cnnStr = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");
            string sql = @"select top 1 * from [DumpData] where mesic = @mesic and rok = @rok and den = @den order by created desc";
            using (var p = new Devmasters.DbConnect())
            {
                var ds = p.ExecuteDataset(cnnStr, CommandType.Text, sql, new IDataParameter[] {
                            new Microsoft.Data.SqlClient.SqlParameter("den", (int)dump.den),
                            new Microsoft.Data.SqlClient.SqlParameter("mesic", (int)dump.mesic),
                            new Microsoft.Data.SqlClient.SqlParameter("rok", (int)dump.rok),
                });
                if (ds.Tables[0].Rows.Count == 0)
                    return ShouldDownloadStatus.Yes;
                DataRow dr = ds.Tables[0].Rows[0];
                string oldHash = (string)dr["hash"];

                if (string.IsNullOrEmpty(oldHash))
                    return ShouldDownloadStatus.Yes;

                if (oldHash != dump.hashDumpu.Value)
                {
                    //if (dump.casGenerovani < DateTime.Now.Date)
                    //    return ShouldDownloadStatus.WaitForData;
                    //else
                    return ShouldDownloadStatus.Yes;
                }

                return ShouldDownloadStatus.No;
            }
        }

        public static void SaveDumpProcessed(XML.indexDump dump, DateTime? processed, Exception ex = null)
        {
            string cnnStr = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");
            string sql = @"INSERT INTO [dbo].[DumpData]
           ([Created]
           ,[Processed]
           ,[den]
           ,[mesic]
           ,[rok]
           ,[hash]
           ,[velikost]
           ,[casGenerovani], exception)
     VALUES
           (@Created
           ,@Processed
           ,@den
           ,@mesic
           ,@rok
           ,@hash
           ,@velikost
           ,@casGenerovani
            ,@exception)
";

            try
            {

                using (var p = new Devmasters.DbConnect())
                {
                    p.ExecuteNonQuery(cnnStr, CommandType.Text, sql, new IDataParameter[] {
                        new Microsoft.Data.SqlClient.SqlParameter("created", DateTime.Now),
                        new Microsoft.Data.SqlClient.SqlParameter("processed", processed),
                        new Microsoft.Data.SqlClient.SqlParameter("den", (int)dump.den),
                        new Microsoft.Data.SqlClient.SqlParameter("mesic", (int)dump.mesic),
                        new Microsoft.Data.SqlClient.SqlParameter("rok", (int)dump.rok),
                        new Microsoft.Data.SqlClient.SqlParameter("hash", dump.hashDumpu.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("velikost", (long)dump.velikostDumpu),
                        new Microsoft.Data.SqlClient.SqlParameter("casGenerovani", dump.casGenerovani),
                        new Microsoft.Data.SqlClient.SqlParameter("exception", ex==null ? (string)null : ex.ToString()),
                        });

                }
            }
            catch (Exception e)
            {

                _logger.Error(e, "SaveDumpProcessed error");
            }

        }


        public class XML
        {




            /// <remarks/>
            [SerializableAttribute()]
            [System.ComponentModel.DesignerCategoryAttribute("code")]
            [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/")]
            [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/", IsNullable = false)]
            public partial class index
            {

                private indexDump[] dumpField;

                /// <remarks/>
                [System.Xml.Serialization.XmlElementAttribute("dump")]
                public indexDump[] dump
                {
                    get
                    {
                        return dumpField;
                    }
                    set
                    {
                        dumpField = value;
                    }
                }
            }

            /// <remarks/>
            [SerializableAttribute()]
            [System.ComponentModel.DesignerCategoryAttribute("code")]
            [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/")]
            public partial class indexDump
            {
                public DateTime ForDate()
                {
                    if (den == 0)
                        return new DateTime(rok, mesic, 1);
                    else
                        return new DateTime(rok, mesic, den);
                }

                private byte denField;

                private byte mesicField;

                private ushort rokField;

                private indexDumpHashDumpu hashDumpuField;

                private uint velikostDumpuField;

                private DateTime casGenerovaniField;

                private byte dokoncenyMesicField;

                private string odkazField;

                public byte den
                {
                    get
                    {
                        return denField;
                    }
                    set
                    {
                        denField = value;
                    }
                }

                /// <remarks/>
                public byte mesic
                {
                    get
                    {
                        return mesicField;
                    }
                    set
                    {
                        mesicField = value;
                    }
                }

                /// <remarks/>
                public ushort rok
                {
                    get
                    {
                        return rokField;
                    }
                    set
                    {
                        rokField = value;
                    }
                }

                /// <remarks/>
                public indexDumpHashDumpu hashDumpu
                {
                    get
                    {
                        return hashDumpuField;
                    }
                    set
                    {
                        hashDumpuField = value;
                    }
                }

                /// <remarks/>
                public uint velikostDumpu
                {
                    get
                    {
                        return velikostDumpuField;
                    }
                    set
                    {
                        velikostDumpuField = value;
                    }
                }

                /// <remarks/>
                public DateTime casGenerovani
                {
                    get
                    {
                        return casGenerovaniField;
                    }
                    set
                    {
                        casGenerovaniField = value;
                    }
                }

                /// <remarks/>
                public byte dokoncenyMesic
                {
                    get
                    {
                        return dokoncenyMesicField;
                    }
                    set
                    {
                        dokoncenyMesicField = value;
                    }
                }

                /// <remarks/>
                public string odkaz
                {
                    get
                    {
                        return odkazField;
                    }
                    set
                    {
                        odkazField = value;
                    }
                }
            }

            /// <remarks/>
            [SerializableAttribute()]
            [System.ComponentModel.DesignerCategoryAttribute("code")]
            [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://portal.gov.cz/rejstriky/ISRS/1.2/")]
            public partial class indexDumpHashDumpu
            {

                private string algoritmusField;

                private string valueField;

                /// <remarks/>
                [System.Xml.Serialization.XmlAttributeAttribute()]
                public string algoritmus
                {
                    get
                    {
                        return algoritmusField;
                    }
                    set
                    {
                        algoritmusField = value;
                    }
                }

                /// <remarks/>
                [System.Xml.Serialization.XmlTextAttribute()]
                public string Value
                {
                    get
                    {
                        return valueField;
                    }
                    set
                    {
                        valueField = value;
                    }
                }
            }


        }

    }
}
