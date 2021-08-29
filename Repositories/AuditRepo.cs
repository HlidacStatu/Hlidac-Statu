using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

using System;

namespace HlidacStatu.Repositories
{
    public static class AuditRepo
    {

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

                var res = Manager.GetESClient_Audit()
                    .Index<Audit>(a, m => m.Id(a.Id));

                return a;
            }
            catch
            {
                return null;
            }

        }
    }
}