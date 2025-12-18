using HlidacStatu.Entities;
using System;
using Serilog;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class AuditRepo
    {

        private static readonly ILogger _logger = Log.ForContext(typeof(AuditRepo));

        public static Audit Add<T>(Audit.Operations operation, string user, T newObj, T prevObj)
            where T : IAuditable
        {
            return Add<T>(operation, user, null, newObj, prevObj);
        }
        public static Audit Add<T>(Audit.Operations operation, string user, string ipAddress, T newObj, T prevObj)
            where T : IAuditable
        {
            return Add(operation, user, ipAddress,
                newObj?.ToAuditObjectId(), newObj?.ToAuditObjectTypeName(),
                newObj?.ToAuditJson(), prevObj?.ToAuditJson()
            );
        }

        public static TResult AddWithElapsedTimeMeasure<TResult>(Audit.Operations operation, string user, string ipAddress,
            string objectId, string objectType,
            string newObjSer, string prevObjSer, Func<TResult> codeToExecute)
        {
            Audit a = new Audit();
            a.date = DateTime.Now;
            a.objectId = objectId;
            a.objectType = objectType;
            a.operation = operation.ToString();
            a.IP = ipAddress;
            a.userId = user ?? "";
            a.valueBefore = prevObjSer;
            a.valueAfter = newObjSer ?? "";

            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            TResult res = default;
            try
            {
                sw.Start();
                res = codeToExecute();

            }
            catch (Exception e)
            {
                a.exception = e.ToString();
            }
            finally
            {
                sw.Stop();
                a.timeElapsedInMs = sw.ElapsedMilliseconds;
                _ = Add(a);
            }
            return res;

        }
        public async static Task<TResult> AddWithElapsedTimeMeasureAsync<TResult>(Audit.Operations operation, string user, string ipAddress,
          string objectId, string objectType,
          string newObjSer, string prevObjSer, Func<Task<TResult>> codeToExecuteAsync)
        {
            Audit a = new Audit();
            a.date = DateTime.Now;
            a.objectId = objectId;
            a.objectType = objectType;
            a.operation = operation.ToString();
            a.IP = ipAddress;
            a.userId = user ?? "";
            a.valueBefore = prevObjSer;
            a.valueAfter = newObjSer ?? "";

            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            TResult res = default;
            try
            {
                sw.Start();
                res = await codeToExecuteAsync().ConfigureAwait(false);

            }
            catch (Exception e)
            {
                a.exception = e.ToString();
            }
            finally
            {
                sw.Stop();
                a.timeElapsedInMs = sw.ElapsedMilliseconds;
                _ = Add(a);
            }
            return res;

        }
        public static Audit Add(Audit.Operations operation, string user, string ipAddress,
            string objectId, string objectType,
            string newObjSer, string prevObjSer, long timeElapsedInMs = 0)
        {
            try
            {
                var a = new Audit();
                a.date = DateTime.Now;
                a.objectId = objectId;
                a.objectType = objectType;
                a.operation = operation.ToString();
                a.IP = ipAddress;
                a.userId = user ?? "";
                a.valueBefore = prevObjSer;
                a.valueAfter = newObjSer ?? "";
                a.timeElapsedInMs = timeElapsedInMs;

                if (operation == Audit.Operations.Search)
                {
                    return a;
                }

                switch (operation)
                {
                    case Audit.Operations.Read:
                    case Audit.Operations.Update:
                    case Audit.Operations.Delete:
                    case Audit.Operations.Create:
                    case Audit.Operations.Other:
                    case Audit.Operations.Search:
                    case Audit.Operations.UserSearch:
                        _logger.Debug("{@audit}", a);

                        break;
                    case Audit.Operations.Call:
                        _logger.Information("{@audit}", a);
                        break;
                    case Audit.Operations.InvalidAccess:
                        _logger.Warning("{@audit}", a);
                        break;
                    default:
                        break;
                }
                //var res = Manager.GetESClient_Audit()
                //    .Index<Audit>(a, m => m.Id(a.Id));

                return a;
            }
            catch
            {
                return null;
            }

        }

        public static Audit Add(Audit audit)
        {
            if (Enum.TryParse<Audit.Operations>(audit.operation, out var operation))
            {
                try
                {
                    switch (operation)
                    {
                        case Audit.Operations.Read:
                        case Audit.Operations.Update:
                        case Audit.Operations.Delete:
                        case Audit.Operations.Create:
                        case Audit.Operations.Other:
                        case Audit.Operations.Search:
                        case Audit.Operations.UserSearch:
                            _logger.Debug("{@audit}", audit);

                            break;
                        case Audit.Operations.Call:
                            _logger.Information("{@audit}", audit);
                            break;
                        case Audit.Operations.InvalidAccess:
                            _logger.Warning("{@audit}", audit);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Chyba při logování do auditu.");
                    return null;
                }
            }

            return audit;

        }
        public static string GetClassAndMethodName(MethodBase method)
        {
            if (method == null)
                return "";
            var className = method.DeclaringType?.Name ?? "";
            var methodName = method.Name;
            return $"{className}.{methodName}";
        }
        public static string GetMethodParametersWithValues(IEnumerable<ParameterInfo> parameters, params object[] args)
        {
            if (parameters == null)
                return "";
            var arrParams = parameters.ToArray();
            List<string> list = new List<string>();
            for (int i = 0; i < Math.Min(arrParams.Length, args.Length); i++)
            {
                list.Add($"{arrParams[i].Name}={args[i]?.ToString()}");
            }

            return string.Join("|", list);
        }
    }
}