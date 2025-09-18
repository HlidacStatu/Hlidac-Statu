using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using HlidacStatu.Entities.Entities.PoliticiSelfAdmin;

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
                .UseSqlServer(connectionString) //, sql=> sql.CommandTimeout(120).EnableRetryOnFailure(2) )
                //.EnableDetailedErrors()  //ukáže který sloupec je null/nejde deserializovat v chybě
                //.EnableSensitiveDataLogging(true)

                .Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //_ = modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            _ = modelBuilder.Entity<CenyCustomer>().HasKey(u => new
            {
                u.Username,
                u.Analyza,
                u.Rok
            });

            _ = modelBuilder.Entity<MonitoredTask>().HasKey(u => new
            {
                u.Pk
            });

            _ = modelBuilder.Entity<AutocompleteSynonym>(entity =>
            {
                entity.Property(e => e.Active).HasDefaultValueSql("((1))");

                entity.Property(e => e.ImageElement).HasDefaultValueSql("('')");

                entity.Property(e => e.Type).HasDefaultValueSql("('')");
            });

            _ = modelBuilder.Entity<BannedIp>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<Feedback>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<Bookmark>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Folder).HasDefaultValueSql("('')");

                entity.Property(e => e.Url).IsUnicode(false);
            });

            _ = modelBuilder.Entity<EventType>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            _ = modelBuilder.Entity<FirmaNace>(entity =>
            {
                entity.HasKey(e => new { e.Ico, e.Nace });
            });

            _ = modelBuilder.Entity<FirmaVazby>(entity =>
            {
                entity.Property(e => e.LastUpdate).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<ItemToOcrQueue>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<QVoiceToText>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<QTblsInDoc>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<QAITask>(entity =>
            {
                entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<KodPf>(entity =>
            {
                entity.Property(e => e.Kod).ValueGeneratedNever();
            });

            _ = modelBuilder.Entity<Osoba>(entity =>
            {
                _ = entity.HasKey(e => e.InternalId)
                    .HasName("PK_Osoba_2");

                _ = entity.Property(e => e.LastUpdate).HasDefaultValueSql("(getdate())");

                _ = entity.Property(e => e.Pohlavi)
                    .IsUnicode(false)
                    .IsFixedLength(true);
            });

            _ = modelBuilder.Entity<Firma>(entity =>
            {
                _ = entity.HasKey(e => e.ICO)
                    .HasName("PK_Firma");

                _ = entity.Property(e => e.VersionUpdate).HasDefaultValue<int>(0);
                _ = entity.Property(e => e.PocetZamKod).HasDefaultValue<int?>(0);
                

            });

            _ = modelBuilder.Entity<OsobaEvent>(entity =>
            {
                _ = entity.HasKey(e => e.Pk)
                    .HasName("PK_OsobaEvent_1");

                _ = entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });


            _ = modelBuilder.Entity<OsobaExternalId>(entity =>
            {
                _ = entity.HasKey(e => new { e.OsobaId, e.ExternalId, e.ExternalSource });
            });

            _ = modelBuilder.Entity<OsobaVazby>(entity =>
            {
                _ = entity.Property(e => e.LastUpdate).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<Review>(entity =>
            {
                _ = entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<SmlouvyId>(entity =>
            {
                _ = entity.Property(e => e.Active).HasDefaultValueSql("((1))");


                _ = entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");

                _ = entity.Property(e => e.Updated).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<Ssmq>(entity =>
            {
                _ = entity.Property(e => e.Pk).ValueGeneratedNever();

                _ = entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");

                _ = entity.Property(e => e.Priority).HasDefaultValueSql("((1))");
            });

            _ = modelBuilder.Entity<TipUrl>(entity =>
            {
                _ = entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<UserOptions>(entity =>
            {
                _ = entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });


            _ = modelBuilder.Entity<WatchDog>(entity =>
            {
                _ = entity.Property(e => e.ToEmail).HasDefaultValueSql("((1))");
            });

            _ = modelBuilder.Entity<ZkratkaStrany>(entity =>
            {
                _ = entity.HasKey(e => e.Ico)
                    .HasName("PK__ZkratkaS__C497141E3346B980");
            });

            // modelBuilder.Entity<InDocTables>(entity =>
            // {
            //     entity.Property(e => e.Category)
            //         .HasConversion<int?>();
            // });
            //
            // modelBuilder.Entity<InDocJobs>(entity =>
            // {
            //     entity.Property(e => e.Unit)
            //         .HasConversion<int>();
            // });

            _ = modelBuilder.Entity<RecalculateItem>(entity =>
            {
                _ = entity.Property(e => e.Created).HasDefaultValueSql("(getdate())");
            });

            _ = modelBuilder.Entity<SmlouvaVerejnaZakazka>()
                .HasKey(s => new { s.VzId, s.IdSmlouvy });


        }

        public virtual DbSet<UptimeServer> UptimeServers { get; set; }

        public virtual DbSet<Cena> Ceny { get; set; }
        public virtual DbSet<MonitoredTask> MonitoredTasks { get; set; }

        public virtual DbSet<CenyCustomer> CenyCustomer { get; set; }

        public virtual DbSet<Firma> Firma { get; set; }
        public virtual DbSet<Feedback> Feedbacks { get; set; }
        public virtual DbSet<InDocTablesCheck> InDocTablesChecks { get; set; }
        
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
        public virtual DbSet<QVoiceToText> QVoiceToText { get; set; }
        public virtual DbSet<QTblsInDoc> QTblsInDoc { get; set; }
        public virtual DbSet<QAITask> QAITask { get; set; }

        public virtual DbSet<EventType> EventType { get; set; }
        public virtual DbSet<OsobaEvent> OsobaEvent { get; set; }
        public virtual DbSet<FirmaHint> FirmaHint { get; set; }
        public virtual DbSet<UcetniJednotka> UcetniJednotka { get; set; }
        public virtual DbSet<TipUrl> TipUrl { get; set; }
        public virtual DbSet<ClassificationOverride> ClassificationOverride { get; set; }
        public virtual DbSet<ZkratkaStrany> ZkratkaStrany { get; set; }
        public virtual DbSet<Sponzoring> Sponzoring { get; set; }
        public virtual DbSet<BannedIp> BannedIps { get; set; }
        public virtual DbSet<InDocJobNameDescription> InDocJobNameDescription { get; set; }

        public virtual DbSet<InDocTag> InDocTags { get; set; }
        public virtual DbSet<InDocJobNames> InDocJobNames { get; set; }
        public virtual DbSet<InDocJobs> InDocJobs { get; set; }
        public virtual DbSet<InDocTables> InDocTables { get; set; }
        public virtual DbSet<FirmaVlastnenaStatem> FirmyVlastneneStatem { get; set; }
        
        public virtual DbSet<AdresaOvm> AdresaOvm { get; set; }
        public virtual DbSet<AdresniMisto> AdresniMisto { get; set; }
        public virtual DbSet<OrganVerejneMoci> OrganVerejneMoci { get; set; }
        public virtual DbSet<PravniFormaOvm> PravniFormaOvm { get; set; }
        public virtual DbSet<TypOvm> TypOvm { get; set; }
        public virtual DbSet<ConfigurationValue> ConfigurationValues { get; set; }
        
        
        public virtual DbSet<FirmaDs> FirmaDs { get; set; }
        public virtual DbSet<PuOrganizace> PuOrganizace { get; set; }
        public virtual DbSet<PuPlat> PuPlaty { get; set; }
        public virtual DbSet<PpPrijem> PpPrijmy { get; set; }

        public virtual DbSet<PuEvent> PuEvents{ get; set; }

        public virtual DbSet<PuVydelek> PuVydelky { get; set; }
        public virtual DbSet<PuCZISCO> PuCZISCO { get; set; }
        public virtual DbSet<PuOrganizaceMetadata> PuOrganizaceMetadata { get; set; }
        public virtual DbSet<PuOrganizaceTag> PuOrganizaceTags { get; set; }
        public virtual DbSet<AutocompleteSynonym> AutocompleteSynonyms { get; set; }
        
        public virtual DbSet<SmlouvaVerejnaZakazka> SmlouvaVerejnaZakazka { get; set; }
        
        public DbSet<PoliticiEditorUser> PoliticiEditorUsers { get; set; }
        public DbSet<PoliticiLoginToken> PoliticiLoginTokens { get; set; }

        //views
        public DbSet<FindPersonDTO> FindPersonView { get; set; }
        public DbSet<SponzoringOverview> SponzoringOverviewView { get; set; }
        public DbSet<SponzoringSummed> SponzoringSummedView { get; set; }
        public DbSet<Ids> IdsView { get; set; }
        public DbSet<UserJobStatistics> UserJobStatistics { get; set; }
        public DbSet<AdresyKVolbam> AdresyKVolbam { get; set; }
        public DbSet<SponzoringDetail> SponzoringDetails { get; set; }

        public DbSet<ObceZUJ> ObceZUJ { get; set; }
        public DbSet<ObceZUJAttr> ObceZUJAttr { get; set; }
        public DbSet<ObceZUJAttrName> ObceZUJAttrName { get; set; }

        public DbSet<RecalculateItem> RecalculateItem { get; set; }
        
        public DbSet<NapojenaOsoba> NapojeneOsobyView { get; set; }
    }
}
