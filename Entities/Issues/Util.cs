using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;

namespace HlidacStatu.Entities.Issues
{
    public class Util
    {

        private static object objLock = new object();
        private static List<IIssueAnalyzer> issueAnalyzers = null;
        private static readonly ILogger _logger = Log.ForContext<Util>();

        public static string IssuesByLevelQuery(ImportanceLevel importanceLevel)
        {
            List<string> sb = new List<string>();
            string logical = " OR ";
            string key = "issues.issueTypeId:{0}";
            foreach (int iss in Enum.GetValues(typeof(IssueType.IssueTypes)))
            {
                if (IssueType.IssueImportance(iss) == importanceLevel)
                    sb.Add(string.Format(key, iss));
            }

            if (sb.Count == 0)
                return string.Empty;
            else
                return "( " + string.Join(logical,sb) + " )";
        }

        public static List<IIssueAnalyzer> GetIssueAnalyzers(string moreFromPath = null)
        {
            if (issueAnalyzers != null)
                return issueAnalyzers;

            lock (objLock)
            {
                if (issueAnalyzers != null)
                    return issueAnalyzers;

                ////load DLL's from disk
                List<System.Reflection.Assembly> dlls = new List<System.Reflection.Assembly>();
                string pathForDlls = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                foreach (var file in System.IO.Directory.EnumerateFiles(pathForDlls, "*plugin*.dll"))
                {
                    try
                    {
                        string filename = System.IO.Path.GetFileNameWithoutExtension(file);
                        dlls.Add(System.Reflection.Assembly.Load(filename));
                    }
                    catch (Exception)
                    {
                    }
                }

                var typeI = typeof(IIssueAnalyzer);
                List<Type> types = null;
                //var t = AppDomain.CurrentDomain.GetAssemblies()
                //    .SelectMany(s =>
                //    {
                //        Type[] tt = null;
                //        try
                //        {
                //            tt = s.GetTypes();
                //        }
                //        catch (Exception e)
                //        {
                //        }
                //        if (tt != null)
                //            return tt.Where(m => m != null).ToArray();
                //        else 
                //        return new Type[] { };
                //    })
                //    .Where(p => p.FullName.StartsWith("HlidacStatu.Plugin.IssueAnalyzers"))
                //    .ToList();

                try
                {
                    types = dlls
                        .SelectMany(s =>
                        {
                            Type[] tt = null;
                            try
                            {
                                tt = s.GetTypes();
                            }
                            catch
                            {
                            }
                            if (tt != null)
                                return tt.Where(m => m != null).ToArray();
                            else
                                return new Type[] { };
                        })
                        .Where(p =>
                            typeI.IsAssignableFrom(p)
                            && p.IsAbstract == false
                            )
                        .ToList();

                }
                catch (System.Reflection.ReflectionTypeLoadException lex)
                {
                    StringBuilder sb = new StringBuilder();
                    if (lex.LoaderExceptions != null)
                        foreach (Exception ex in lex.LoaderExceptions)
                            sb.AppendFormat("{0}\n----------------\n", ex.ToString());

                    _logger.Fatal(lex, "Cannot make list of issueAnalyzer instances, reason: " + sb.ToString());

                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "Cannot make list of issueAnalyzer plugin instances ");

                    throw;
                }

                var ps = new List<IIssueAnalyzer>();
                foreach (var type in types)
                {
                    try
                    {
                        IIssueAnalyzer parser = (IIssueAnalyzer)Activator.CreateInstance(type);
                        ps.Add(parser);
                        _logger.Information("Creating instance of issueAnalyzer plugin " + type.FullName);

                    }
                    catch (Exception)
                    {
                        //NoveInzeraty.Lib.Constants.NIRoot.Error("Cannot make instance of parser " + type.FullName, e);
                    }
                }
                issueAnalyzers = ps;
                return issueAnalyzers;
            }
        }
    }
}
