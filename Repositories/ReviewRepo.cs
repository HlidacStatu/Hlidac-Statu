using System.Threading.Tasks;
using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories
{
    public static class ReviewRepo
    {
        public static async Task SaveAsync(Review review)
        {
            await using DbEntities db = new DbEntities();
            db.Review.Attach(review);
            if (review.Id == 0)
                db.Entry(review).State = EntityState.Added;
            else
                db.Entry(review).State = EntityState.Modified;

            await db.SaveChangesAsync();
        }
    }
}