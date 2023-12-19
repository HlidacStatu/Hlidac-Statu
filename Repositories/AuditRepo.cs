using HlidacStatu.Entities;
using System;
using Serilog;

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

        public static Audit Add(Audit.Operations operation, string user, string ipAddress,
            string objectId, string objectType,
            string newObjSer, string prevObjSer)
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
                    HlidacStatu.Util.Consts.Logger.Error("Chyba při logování do auditu.", ex);
                    return null;
                }
            }

            return audit;

        }
    }
}