using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class FeedbackRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(FeedbackRepo));

        public enum ObjectTypes
        {
            smlouva
        }

        public enum EvaluatedParameters
        {
            smlouva_AI_Summary
        }

        public static Feedback Load(long feedbackPk)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Feedbacks.AsNoTracking().FirstOrDefault(m => m.Pk == feedbackPk);
            }
        }
        public static Feedback Load(string itemId, ObjectTypes? itemObjectType, EvaluatedParameters? itemEvaluatedParameter)
        {
            return Load(itemId, itemObjectType?.ToString(), itemEvaluatedParameter?.ToString());
        }
        public static Feedback Load(string itemId, string itemObjectType = null, string itemEvaluatedParameter = null)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.Feedbacks.AsNoTracking().FirstOrDefault(m =>
                m.ItemId == itemId
                && m.ItemObjectType == itemObjectType
                && m.ItemEvaluatedParameter == itemEvaluatedParameter
                );
            }
        }


        public static void Save(this Feedback feedback)
        {
            using (DbEntities db = new DbEntities())
            {
                if (feedback.Pk == 0)
                {
                    db.Feedbacks.Add(feedback);
                }
                else
                {
                    db.Feedbacks.Attach(feedback);
                    db.Entry(feedback).State = EntityState.Modified;
                }

                db.SaveChanges();
            }
        }
    }
}