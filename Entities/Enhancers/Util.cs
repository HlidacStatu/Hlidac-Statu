using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;

namespace HlidacStatu.Entities.Enhancers
{
    public static class Util
    {

        private static object objLock = new object();
        private static List<IEnhancer> enhancers = null;
        private static readonly ILogger _logger = Log.ForContext(typeof(Util));

        public static List<IEnhancer> GetEnhancers(string moreFromPath = null)
        {
            if (enhancers != null)
                return enhancers;

            lock (objLock)
            {
                if (enhancers != null)
                    return enhancers;

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



                var typeI = typeof(IEnhancer);
                List<Type> types = null;
                try
                {
                    if (false)
                    /* creates exceptions in t3 
                      Unable to load one or more of the requested types.
                      Could not load type 'SqlGuidCaster' from assembly 'Microsoft.Data.SqlClient, Version=5.0.0.0, Culture=neutral, PublicKeyToken=23ec7fc2d6eaa4a5' because it contains an object field at offset 0 that is incorrectly aligned or overlapped by a non-object field.
                     */
                    {
                        var t1 = AppDomain.CurrentDomain.GetAssemblies().ToArray();
                        var t2 = t1.Union(dlls).ToArray();
                        var t3 = t2.SelectMany(s => s.GetTypes()).ToArray();
                        var t4 = t3.Where(p =>
                                typeI.IsAssignableFrom(p)
                                && p.IsAbstract == false
                                ).ToArray();
                        types = t4.ToList();
                    }
                    types = dlls
                        .SelectMany(s => s.GetTypes())
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

                    _logger.Fatal(lex, "Cannot make list of enhancer instances, reason: " + sb.ToString());

                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "Cannot make list of enhancer instances ");

                    throw;
                }

                var ps = new List<IEnhancer>();
                foreach (var type in types)
                {
                    try
                    {
                        IEnhancer parser = (IEnhancer)Activator.CreateInstance(type);
                        ps.Add(parser);
                        if (parser.Name == "FormalNormalizer")
                        {
                            //parser.SetInstanceData(StaticData.CiziStaty);
                        }

                        _logger.Information("Creating instance of enhancer plugin " + type.FullName);

                    }
                    catch (Exception)
                    {
                        //NoveInzeraty.Lib.Constants.NIRoot.Error("Cannot make instance of parser " + type.FullName, e);
                    }
                }
                enhancers = ps;

                return enhancers.OrderByDescending(o => o.Priority).ToList();
            }
        }
    }
}
