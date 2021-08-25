using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    public class CamelotResultWithJobs : CamelotResult
    {

        public CamelotResultWithJobs(CamelotResult cr)
        {
            if (cr == null)
                throw new ArgumentNullException("cr");
            if (cr.Tables != null)
            {
                List<TableWithJobs> tbls = new List<TableWithJobs>();
                foreach (var crT in cr.Tables)
                {
                    TableWithJobs tbl = new TableWithJobs();
                    tbl.Content = crT.Content;
                    tbl.Page = crT.Page;
                    tbl.TableInPage = crT.TableInPage;

                    var score = HlidacStatu.DetectJobs.InHtmlTables.TableWithWordsAndNumbers(
                        tbl.Content, 
                        HlidacStatu.DetectJobs.InHtmlTables.SpecificWords, out var foundJobs, out var cells);
                    if (foundJobs != null)
                        tbl.FoundJobs = foundJobs.ToArray();
                    else
                        tbl.FoundJobs = new DetectJobs.InHtmlTables.Job[] { };
                    tbls.Add(tbl);
                }
                this.TablesWithJobs = tbls.ToArray();
            }
            this.Algorithm = cr.Algorithm;
            this.ElapsedTimeInMs = cr.ElapsedTimeInMs;
            this.Format = cr.Format;
            this.FoundTables = cr.FoundTables;
            this.ScriptOutput = cr.ScriptOutput;
            this.SessionId = cr.SessionId;
            this.Status = cr.Status;
            this.Tables = cr.Tables;

        }

        public class TableWithJobs : Table
        {
            public HlidacStatu.DetectJobs.InHtmlTables.Job[] FoundJobs { get; set; }
        }

        public TableWithJobs[] TablesWithJobs { get; set; } = new TableWithJobs[] { };


    }

}
