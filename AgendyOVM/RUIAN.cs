using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendyOVM
{
    public class RUIAN
    {
        public static string GetAdresaByMistoID(string mistoId)
        {
            string url = "https://ags.cuzk.cz/arcgis/rest/services/RUIAN/Prohlizeci_sluzba_nad_daty_RUIAN/MapServer/1/query?text=&objectIds=&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&distance=&units=esriSRUnit_Foot&relationParam=&outFields=nespravny%2Ccislodomovni%2Ccisloorientacni%2Ccisloorientacnipismeno%2Cpsc%2Culice%2Cplatiod%2Cplatido%2Cadresa&returnGeometry=true&returnTrueCurves=false&maxAllowableOffset=&geometryPrecision=&outSR=&havingClause=&returnIdsOnly=false&returnCountOnly=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&returnZ=false&returnM=false&gdbVersion=&historicMoment=&returnDistinctValues=false&resultOffset=&resultRecordCount=&returnExtentOnly=false&datumTransformation=&parameterValues=&rangeValues=&quantizationParameters=&featureEncoding=esriDefault&f=pjson&where=kod+%3D+"+mistoId;

            try
            {
                string json = new System.Net.WebClient().DownloadString(url);
                ApiResult res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult>(json);

                return res?.features?.FirstOrDefault()?.attributes?.adresa;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }

            
        }


        public class ApiResult
        {
            public string displayFieldName { get; set; }
            public Fieldaliases fieldAliases { get; set; }
            public string geometryType { get; set; }
            public Spatialreference spatialReference { get; set; }
            public Field[] fields { get; set; }
            public Feature[] features { get; set; }


            public class Feature
            {
                public Attributes attributes { get; set; }
                public Geometry geometry { get; set; }
                public class Attributes
                {
                    public string nespravny { get; set; }
                    public string cislodomovni { get; set; }
                    public string cisloorientacni { get; set; }
                    public string cisloorientacnipismeno { get; set; }
                    public string psc { get; set; }
                    public string ulice { get; set; }
                    public string platiod { get; set; }
                    public string platido { get; set; }
                    public string adresa { get; set; }
                }

                public class Geometry
                {
                    public float? x { get; set; }
                    public float? y { get; set; }
                }
            }

            public class Fieldaliases
            {
                public string nespravny { get; set; }
                public string cislodomovni { get; set; }
                public string cisloorientacni { get; set; }
                public string cisloorientacnipismeno { get; set; }
                public string psc { get; set; }
                public string ulice { get; set; }
                public string platiod { get; set; }
                public string platido { get; set; }
                public string adresa { get; set; }
            }

            public class Spatialreference
            {
                public int? wkid { get; set; }
                public int? latestWkid { get; set; }
            }

            public class Field
            {
                public string name { get; set; }
                public string type { get; set; }
                public string alias { get; set; }
                public int? length { get; set; }
            }


        }



    }
}
