using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories
{
    public static class ReviewRepo
    {
        public static void Save(Review review)
        {
            using (DbEntities db = new DbEntities())
            {
                db.Review.Attach(review);
                if (review.Id == 0)
                    db.Entry(review).State = EntityState.Added;
                else
                    db.Entry(review).State = EntityState.Modified;

                db.SaveChanges();
            }
        }
    }
}