using HlidacStatu.Entities.Views;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities
{
    public class DbEntities : IdentityDbContext<ApplicationUser>
    {
        public DbEntities()
            : base(GetConnectionString())
        {
        }

        public DbEntities(DbContextOptions<DbEntities> options)
            : base(options)
        {
        }

        private static DbContextOptions GetConnectionString()
        {
            var connectionString = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");
            return new DbContextOptionsBuilder()
                .UseSqlServer(connectionString)
                //.EnableSensitiveDataLogging(true)
                .Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<Audit>(entity =>
            {
                entity.Property(e => e.date).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<AutocompleteSynonym>(entity =>
            {
                entity.Property(e => e.Active).HasDefaultValueSql("((1))");

                entity.Property(e => e.ImageElement).HasDefaultValueSql("('')");

                entity.Property(e => e.Type).HasDefaultValueSql("('')");
            });

            modelBuilder.Entity<BannedIp>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Bookmark>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Folder).HasDefaultValueSql("('')");

                entity.Property(e => e.Url).IsUnicode(false);
            });

            modelBuilder.Entity<EventType>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<FirmaNace>(entity =>
            {
                entity.HasKey(e => new { e.Ico, e.Nace });
            });

            modelBuilder.Entity<FirmaVazby>(entity =>
            {
                entity.Property(e => e.LastUpdate).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<ItemToOcrQueue>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<KodPf>(entity =>
            {
                entity.Property(e => e.Kod).ValueGeneratedNever();
            });

            modelBuilder.Entity<Osoba>(entity =>
            {
                entity.HasKey(e => e.InternalId)
                    .HasName("PK_Osoba_2");

                entity.Property(e => e.LastUpdate).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Pohlavi)
                    .IsUnicode(false)
                    .IsFixedLength(true);
            });

            modelBuilder.Entity<Firma>(entity =>
            {
                entity.HasKey(e => e.ICO)
                    .HasName("PK_Firma");

                entity.Property(e => e.VersionUpdate).HasDefaultValue<int>(0);
                entity.Property(e => e.PocetZam).HasDefaultValue<int>(0);

            });

            modelBuilder.Entity<OsobaEvent>(entity =>
            {
                entity.HasKey(e => e.Pk)
                    .HasName("PK_OsobaEvent_1");

                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });


            modelBuilder.Entity<OsobaExternalId>(entity =>
            {
                entity.HasKey(e => new { e.OsobaId, e.ExternalId, e.ExternalSource });
            });

            modelBuilder.Entity<OsobaVazby>(entity =>
            {
                entity.Property(e => e.LastUpdate).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<SmlouvyId>(entity =>
            {
                entity.Property(e => e.Active).HasDefaultValueSql("((1))");

                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Updated).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Ssmq>(entity =>
            {
                entity.Property(e => e.Pk).ValueGeneratedNever();

                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Priority).HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<TipUrl>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<UserOptions>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });


            modelBuilder.Entity<WatchDog>(entity =>
            {
                entity.Property(e => e.ToEmail).HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<ZkratkaStrany>(entity =>
            {
                entity.HasKey(e => e.Ico)
                    .HasName("PK__ZkratkaS__C497141E3346B980");
            });

        }
        public virtual DbSet<Firma> Firma { get; set; }

        public virtual DbSet<WatchDog> WatchDogs { get; set; }
        public virtual DbSet<AspNetUserApiToken> AspNetUserApiTokens { get; set; }
        public virtual DbSet<SmlouvyId> SmlouvyIds { get; set; }
        public virtual DbSet<FirmaVazby> FirmaVazby { get; set; }
        public virtual DbSet<Osoba> Osoba { get; set; } //continue here
        public virtual DbSet<OsobaExternalId> OsobaExternalId { get; set; }
        public virtual DbSet<OsobaVazby> OsobaVazby { get; set; }
        public virtual DbSet<NespolehlivyPlatceDPH> NespolehlivyPlatceDPH { get; set; }
        public virtual DbSet<Review> Review { get; set; }
        public virtual DbSet<Bookmark> Bookmarks { get; set; }
        public virtual DbSet<UserOptions> UserOptions { get; set; }
        public virtual DbSet<ItemToOcrQueue> ItemToOcrQueue { get; set; }
        public virtual DbSet<EventType> EventType { get; set; }
        public virtual DbSet<OsobaEvent> OsobaEvent { get; set; }
        public virtual DbSet<FirmaHint> FirmaHint { get; set; }
        public virtual DbSet<UcetniJednotka> UcetniJednotka { get; set; }
        public virtual DbSet<TipUrl> TipUrl { get; set; }
        public virtual DbSet<ClassificationOverride> ClassificationOverride { get; set; }
        public virtual DbSet<ZkratkaStrany> ZkratkaStrany { get; set; }
        public virtual DbSet<Sponzoring> Sponzoring { get; set; }
        public virtual DbSet<BannedIp> BannedIps { get; set; }

        public virtual DbSet<InDocJobNames> InDocJobNames { get; set; }
        public virtual DbSet<InDocJobs> InDocJobs { get; set; }
        public virtual DbSet<InDocTables> InDocTables { get; set; }
        public virtual DbSet<FirmaVlastnenaStatem> FirmyVlastneneStatem { get; set; }

        //views
        public DbSet<FindPersonDTO> FindPersonView { get; set; }
        public DbSet<SponzoringOverview> SponzoringOverviewView { get; set; }
        public DbSet<SponzoringSummed> SponzoringSummedView { get; set; }
        public DbSet<JobOverview> JobsOverviewView { get; set; }


    }
}
