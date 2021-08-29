using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Lib.Db.Insolvence
{

    public partial class InsolvenceEntities : DbContext
    {
        public InsolvenceEntities()
            : base(GetConnectionString())
        {
        }

        private static DbContextOptions GetConnectionString()
        {
            var connectionString = Devmasters.Config.GetWebConfigValue("InsolvenceEntities");
            return new DbContextOptionsBuilder()
                .UseSqlServer(connectionString)
                .Options;
        }

        // protected override void OnModelCreating(DbModelBuilder modelBuilder)
        // {
        //     throw new UnintentionalCodeFirstException();
        // }

        public virtual DbSet<Dokumenty> Dokumenty { get; set; }
        public virtual DbSet<Rizeni> Rizeni { get; set; }
        public virtual DbSet<Spravci> Spravci { get; set; }
        public virtual DbSet<Veritele> Veritele { get; set; }
        public virtual DbSet<Dluznici> Dluznici { get; set; }
    }
}
