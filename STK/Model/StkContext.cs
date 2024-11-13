using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace STK.Model;

public partial class StkContext : DbContext
{
    public StkContext()
        : base(GetConnectionString())
    {
    }

    public StkContext(DbContextOptions<StkContext> options)
        : base(options)
    {
    }
    private static DbContextOptions GetConnectionString()
    {
        var connectionString = Devmasters.Config.GetWebConfigValue("STKConnection");
        return new DbContextOptionsBuilder()
            .UseSqlServer(connectionString, sql => sql.CommandTimeout(120).EnableRetryOnFailure(2))
            //.EnableDetailedErrors()  //ukáže který sloupec je null/nejde deserializovat v chybě
            //.EnableSensitiveDataLogging(true)

            .Options;
    }

    public virtual DbSet<SmeDefekt> SmeDefekts { get; set; }

    public virtual DbSet<SmeKontrola> SmeKontrolas { get; set; }

    public virtual DbSet<SmeKontrolaBenzin> SmeKontrolaBenzins { get; set; }

    public virtual DbSet<SmeKontrolaDiesel> SmeKontrolaDiesels { get; set; }

    public virtual DbSet<SmeKontrolaPoznamka> SmeKontrolaPoznamkas { get; set; }

    public virtual DbSet<SmeMereniBenzin> SmeMereniBenzins { get; set; }

    public virtual DbSet<SmeMereniDiesel> SmeMereniDiesels { get; set; }

    public virtual DbSet<SmeMereniPlyn> SmeMereniPlyns { get; set; }

    public virtual DbSet<SmePristroj> SmePristrojs { get; set; }

    public virtual DbSet<SmeReadiness> SmeReadinesses { get; set; }

    public virtual DbSet<SmeSondum> SmeSonda { get; set; }

    public virtual DbSet<SmeStanice> SmeStanices { get; set; }

    public virtual DbSet<StkStanice> StkStanices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Czech_CI_AS");

        modelBuilder.Entity<SmeDefekt>(entity =>
        {
            entity.HasKey(e => e.KontrolaDefektId);

            entity.ToTable("SmeDefekt");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeDefekt_Kontrola");

            entity.Property(e => e.KontrolaDefektId).ValueGeneratedNever();
            entity.Property(e => e.Kod).HasMaxLength(50);

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeDefekts)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeDefekt_SmeKontrola");
        });

        modelBuilder.Entity<SmeKontrola>(entity =>
        {
            entity.HasKey(e => e.KontrolaId);

            entity.ToTable("SmeKontrola");

            entity.HasIndex(e => e.Druh, "IX_SmeKontrola_Druh");

            entity.HasIndex(e => e.Kategorie, "IX_SmeKontrola_Kategorie");

            entity.HasIndex(e => new { e.FileYear, e.FileMonth }, "IX_SmeKontrola_Month");

            entity.HasIndex(e => e.PristrojId, "IX_SmeKontrola_Pristroj");

            entity.HasIndex(e => e.Protokol, "IX_SmeKontrola_Protokol");

            entity.HasIndex(e => e.Poradi, "IX_SmeKontrola_Rank");

            entity.HasIndex(e => e.Sme, "IX_SmeKontrola_SME");

            entity.HasIndex(e => e.Spz, "IX_SmeKontrola_SPZ");

            entity.HasIndex(e => e.Technik, "IX_SmeKontrola_Technik");

            entity.HasIndex(e => e.Typ, "IX_SmeKontrola_Typ");

            entity.HasIndex(e => e.Vin, "IX_SmeKontrola_VIN");

            entity.HasIndex(e => e.DetVin, "IX_SmeKontrola_VIND");

            entity.HasIndex(e => e.RecVin, "IX_SmeKontrola_VINH");

            entity.HasIndex(e => e.Vysledek, "IX_SmeKontrola_Vysledek");

            entity.Property(e => e.KontrolaId).ValueGeneratedNever();
            entity.Property(e => e.CisloMotoru).HasMaxLength(25);
            entity.Property(e => e.CisloTp)
                .HasMaxLength(25)
                .HasColumnName("CisloTP");
            entity.Property(e => e.DetModel).HasMaxLength(50);
            entity.Property(e => e.DetPalivo).HasMaxLength(15);
            entity.Property(e => e.DetVin)
                .HasMaxLength(25)
                .HasColumnName("DetVIN");
            entity.Property(e => e.DetVinok).HasColumnName("DetVINok");
            entity.Property(e => e.DetZnacka).HasMaxLength(50);
            entity.Property(e => e.Druh).HasMaxLength(30);
            entity.Property(e => e.Ecu)
                .HasMaxLength(500)
                .HasColumnName("ECU");
            entity.Property(e => e.Kategorie).HasMaxLength(15);
            entity.Property(e => e.Mil)
                .HasMaxLength(1)
                .HasColumnName("MIL");
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.Palivo).HasMaxLength(15);
            entity.Property(e => e.Pristi).HasColumnType("datetime");
            entity.Property(e => e.Protokol)
                .HasMaxLength(30)
                .HasDefaultValue("");
            entity.Property(e => e.RecModel).HasMaxLength(50);
            entity.Property(e => e.RecPalivo).HasMaxLength(15);
            entity.Property(e => e.RecVin)
                .HasMaxLength(25)
                .HasColumnName("RecVIN");
            entity.Property(e => e.RecVinok).HasColumnName("RecVINok");
            entity.Property(e => e.RecZnacka).HasMaxLength(50);
            entity.Property(e => e.Registrace).HasColumnType("datetime");
            entity.Property(e => e.Sme).HasColumnName("SME");
            entity.Property(e => e.Spz)
                .HasMaxLength(30)
                .HasColumnName("SPZ");
            entity.Property(e => e.Stari).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StavEcu)
                .HasMaxLength(1)
                .HasColumnName("StavECU");
            entity.Property(e => e.Technik).HasMaxLength(30);
            entity.Property(e => e.Time1).HasColumnType("datetime");
            entity.Property(e => e.Time2).HasColumnType("datetime");
            entity.Property(e => e.TypMotoru).HasMaxLength(50);
            entity.Property(e => e.Vin)
                .HasMaxLength(25)
                .HasColumnName("VIN");
            entity.Property(e => e.Vinok).HasColumnName("VINok");
            entity.Property(e => e.Znacka).HasMaxLength(50);
        });

        modelBuilder.Entity<SmeKontrolaBenzin>(entity =>
        {
            entity.HasKey(e => e.KontrolaBenzinId);

            entity.ToTable("SmeKontrolaBenzin");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeKontrolaBenzin_Kontrola");

            entity.HasIndex(e => e.Otacky, "IX_SmeKontrolaBenzin_Otacky");

            entity.Property(e => e.KontrolaBenzinId).ValueGeneratedNever();
            entity.Property(e => e.Co2hodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Hodnota");
            entity.Property(e => e.Co2max)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Max");
            entity.Property(e => e.Co2min)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Min");
            entity.Property(e => e.Co2vysledek).HasColumnName("CO2Vysledek");
            entity.Property(e => e.CocoorHodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorHodnota");
            entity.Property(e => e.CocoorMax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorMax");
            entity.Property(e => e.CocoorMin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorMin");
            entity.Property(e => e.CocoorVysledek).HasColumnName("COcoorVysledek");
            entity.Property(e => e.Cohodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COHodnota");
            entity.Property(e => e.Comax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COMax");
            entity.Property(e => e.Comin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COMin");
            entity.Property(e => e.Covysledek).HasColumnName("COVysledek");
            entity.Property(e => e.Hchodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCHodnota");
            entity.Property(e => e.Hcmax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCMax");
            entity.Property(e => e.Hcmin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCMin");
            entity.Property(e => e.Hcvysledek).HasColumnName("HCVysledek");
            entity.Property(e => e.LambdaHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LambdaMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LambdaMin).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Nhodnota).HasColumnName("NHodnota");
            entity.Property(e => e.Nmax).HasColumnName("NMax");
            entity.Property(e => e.Nmin).HasColumnName("NMin");
            entity.Property(e => e.NoxHodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxHodnota");
            entity.Property(e => e.NoxMax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxMax");
            entity.Property(e => e.NoxMin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxMin");
            entity.Property(e => e.NoxVysledek).HasColumnName("NOxVysledek");
            entity.Property(e => e.Nvysledek).HasColumnName("NVysledek");
            entity.Property(e => e.O2hodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Hodnota");
            entity.Property(e => e.O2max)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Max");
            entity.Property(e => e.O2min)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Min");
            entity.Property(e => e.O2vysledek).HasColumnName("O2Vysledek");
            entity.Property(e => e.TpsHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TpsMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TpsMin).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeKontrolaBenzins)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeKontrolaBenzin_SmeKontrola");
        });

        modelBuilder.Entity<SmeKontrolaDiesel>(entity =>
        {
            entity.HasKey(e => e.KontrolaDieselId);

            entity.ToTable("SmeKontrolaDiesel");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeKontrolaDiesel_Kontrola");

            entity.Property(e => e.KontrolaDieselId).ValueGeneratedNever();
            entity.Property(e => e.AbsorbceHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.AkceleraceHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.AkceleraceLimitMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.KourivostHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.KourivostLimitMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.KourivostRozpetiHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.KourivostRozpetiLimitMax).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeKontrolaDiesels)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeKontrolaDiesel_SmeKontrola");
        });

        modelBuilder.Entity<SmeKontrolaPoznamka>(entity =>
        {
            entity.HasKey(e => e.KontrolaPoznamkaId);

            entity.ToTable("SmeKontrolaPoznamka");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeKontrolaPoznamka_Kontrola");

            entity.Property(e => e.KontrolaPoznamkaId).ValueGeneratedNever();

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeKontrolaPoznamkas)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeKontrolaPoznamka_SmeKontrola");
        });

        modelBuilder.Entity<SmeMereniBenzin>(entity =>
        {
            entity.HasKey(e => e.MereniBenzinId);

            entity.ToTable("SmeMereniBenzin");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeMereniBenzin_Kontrola");

            entity.Property(e => e.MereniBenzinId).ValueGeneratedNever();
            entity.Property(e => e.Co2hodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Hodnota");
            entity.Property(e => e.Co2max)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Max");
            entity.Property(e => e.Co2min)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Min");
            entity.Property(e => e.Co2vysledek).HasColumnName("CO2Vysledek");
            entity.Property(e => e.CocoorHodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorHodnota");
            entity.Property(e => e.CocoorMax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorMax");
            entity.Property(e => e.CocoorMin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorMin");
            entity.Property(e => e.CocoorVysledek).HasColumnName("COcoorVysledek");
            entity.Property(e => e.Cohodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COHodnota");
            entity.Property(e => e.Comax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COMax");
            entity.Property(e => e.Comin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COMin");
            entity.Property(e => e.Covysledek).HasColumnName("COVysledek");
            entity.Property(e => e.Hchodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCHodnota");
            entity.Property(e => e.Hcmax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCMax");
            entity.Property(e => e.Hcmin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCMin");
            entity.Property(e => e.Hcvysledek).HasColumnName("HCVysledek");
            entity.Property(e => e.LambdaHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LambdaMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LambdaMin).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Nhodnota).HasColumnName("NHodnota");
            entity.Property(e => e.Nmax).HasColumnName("NMax");
            entity.Property(e => e.Nmin).HasColumnName("NMin");
            entity.Property(e => e.NoxHodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxHodnota");
            entity.Property(e => e.NoxMax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxMax");
            entity.Property(e => e.NoxMin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxMin");
            entity.Property(e => e.NoxVysledek).HasColumnName("NOxVysledek");
            entity.Property(e => e.Nvysledek).HasColumnName("NVysledek");
            entity.Property(e => e.O2hodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Hodnota");
            entity.Property(e => e.O2max)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Max");
            entity.Property(e => e.O2min)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Min");
            entity.Property(e => e.O2vysledek).HasColumnName("O2Vysledek");
            entity.Property(e => e.Palivo)
                .HasMaxLength(15)
                .HasDefaultValue("");
            entity.Property(e => e.TpsHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TpsMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TpsMin).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeMereniBenzins)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeMereniBenzin_SmeKontrola");
        });

        modelBuilder.Entity<SmeMereniDiesel>(entity =>
        {
            entity.HasKey(e => e.MereniDieselId);

            entity.ToTable("SmeMereniDiesel");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeMereniDiesel_Kontrola");

            entity.Property(e => e.MereniDieselId).ValueGeneratedNever();
            entity.Property(e => e.AkceleraceHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.KourivostHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TpsHodnota).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeMereniDiesels)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeMereniDiesel_SmeKontrola");
        });

        modelBuilder.Entity<SmeMereniPlyn>(entity =>
        {
            entity.HasKey(e => e.MereniPlynId);

            entity.ToTable("SmeMereniPlyn");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeMereniPlyn_Kontrola");

            entity.Property(e => e.MereniPlynId).ValueGeneratedNever();
            entity.Property(e => e.Co2hodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Hodnota");
            entity.Property(e => e.Co2max)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Max");
            entity.Property(e => e.Co2min)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CO2Min");
            entity.Property(e => e.Co2vysledek).HasColumnName("CO2Vysledek");
            entity.Property(e => e.CocoorHodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorHodnota");
            entity.Property(e => e.CocoorMax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorMax");
            entity.Property(e => e.CocoorMin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COcoorMin");
            entity.Property(e => e.CocoorVysledek).HasColumnName("COcoorVysledek");
            entity.Property(e => e.Cohodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COHodnota");
            entity.Property(e => e.Comax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COMax");
            entity.Property(e => e.Comin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("COMin");
            entity.Property(e => e.Covysledek).HasColumnName("COVysledek");
            entity.Property(e => e.Hchodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCHodnota");
            entity.Property(e => e.Hcmax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCMax");
            entity.Property(e => e.Hcmin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("HCMin");
            entity.Property(e => e.Hcvysledek).HasColumnName("HCVysledek");
            entity.Property(e => e.LambdaHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LambdaMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LambdaMin).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.NadrzHomologace).HasMaxLength(100);
            entity.Property(e => e.NadrzKontrola).HasMaxLength(15);
            entity.Property(e => e.NadrzVyrobce).HasMaxLength(100);
            entity.Property(e => e.NadrzZivotnost).HasMaxLength(15);
            entity.Property(e => e.Nhodnota).HasColumnName("NHodnota");
            entity.Property(e => e.Nmax).HasColumnName("NMax");
            entity.Property(e => e.Nmin).HasColumnName("NMin");
            entity.Property(e => e.NoxHodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxHodnota");
            entity.Property(e => e.NoxMax)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxMax");
            entity.Property(e => e.NoxMin)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("NOxMin");
            entity.Property(e => e.NoxVysledek).HasColumnName("NOxVysledek");
            entity.Property(e => e.Nvysledek).HasColumnName("NVysledek");
            entity.Property(e => e.O2hodnota)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Hodnota");
            entity.Property(e => e.O2max)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Max");
            entity.Property(e => e.O2min)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("O2Min");
            entity.Property(e => e.O2vysledek).HasColumnName("O2Vysledek");
            entity.Property(e => e.Palivo)
                .HasMaxLength(15)
                .HasDefaultValue("");
            entity.Property(e => e.TpsHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TpsMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TpsMin).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeMereniPlyns)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeMereniPlyn_SmeKontrola");
        });

        modelBuilder.Entity<SmePristroj>(entity =>
        {
            entity.HasKey(e => e.PristrojId);

            entity.ToTable("SmePristroj");

            entity.Property(e => e.PristrojId).ValueGeneratedNever();
            entity.Property(e => e.Obd)
                .HasMaxLength(100)
                .HasColumnName("OBD");
            entity.Property(e => e.Software).HasMaxLength(100);
            entity.Property(e => e.Typ).HasMaxLength(50);
            entity.Property(e => e.Verze).HasMaxLength(100);
            entity.Property(e => e.Vyrobce).HasMaxLength(100);
        });

        modelBuilder.Entity<SmeReadiness>(entity =>
        {
            entity.HasKey(e => e.ReadinessId);

            entity.ToTable("SmeReadiness");

            entity.HasIndex(e => e.KontrolaId, "IX_SmeReadiness_Kontrola");

            entity.Property(e => e.ReadinessId).ValueGeneratedNever();
            entity.Property(e => e.AcS).HasColumnName("AC_s");
            entity.Property(e => e.AcT).HasColumnName("AC_t");
            entity.Property(e => e.BoostS).HasColumnName("BOOST_s");
            entity.Property(e => e.BoostT).HasColumnName("BOOST_t");
            entity.Property(e => e.CatfuncS).HasColumnName("CATFUNC_s");
            entity.Property(e => e.CatfuncT).HasColumnName("CATFUNC_t");
            entity.Property(e => e.ColdS).HasColumnName("COLD_s");
            entity.Property(e => e.ColdT).HasColumnName("COLD_t");
            entity.Property(e => e.CompS).HasColumnName("COMP_s");
            entity.Property(e => e.CompT).HasColumnName("COMP_t");
            entity.Property(e => e.DpfS).HasColumnName("DPF_s");
            entity.Property(e => e.DpfT).HasColumnName("DPF_t");
            entity.Property(e => e.EgrvvtS).HasColumnName("EGRVVT_s");
            entity.Property(e => e.EgrvvtT).HasColumnName("EGRVVT_t");
            entity.Property(e => e.EgsS).HasColumnName("EGS_s");
            entity.Property(e => e.EgsT).HasColumnName("EGS_t");
            entity.Property(e => e.EgsfuncS).HasColumnName("EGSFUNC_s");
            entity.Property(e => e.EgsfuncT).HasColumnName("EGSFUNC_t");
            entity.Property(e => e.EgsheatS).HasColumnName("EGSHEAT_s");
            entity.Property(e => e.EgsheatT).HasColumnName("EGSHEAT_t");
            entity.Property(e => e.EvapS).HasColumnName("EVAP_s");
            entity.Property(e => e.EvapT).HasColumnName("EVAP_t");
            entity.Property(e => e.FuelS).HasColumnName("FUEL_s");
            entity.Property(e => e.FuelT).HasColumnName("FUEL_t");
            entity.Property(e => e.HcatS).HasColumnName("HCAT_s");
            entity.Property(e => e.HcatT).HasColumnName("HCAT_t");
            entity.Property(e => e.MisfS).HasColumnName("MISF_s");
            entity.Property(e => e.MisfT).HasColumnName("MISF_t");
            entity.Property(e => e.NmhcS).HasColumnName("NMHC_s");
            entity.Property(e => e.NmhcT).HasColumnName("NMHC_t");
            entity.Property(e => e.NoxS).HasColumnName("NOX_s");
            entity.Property(e => e.NoxT).HasColumnName("NOX_t");
            entity.Property(e => e.O2sfuncS).HasColumnName("O2SFUNC_s");
            entity.Property(e => e.O2sfuncT).HasColumnName("O2SFUNC_t");
            entity.Property(e => e.O2sheatS).HasColumnName("O2SHEAT_s");
            entity.Property(e => e.O2sheatT).HasColumnName("O2SHEAT_t");
            entity.Property(e => e.ReserveS).HasColumnName("RESERVE_s");
            entity.Property(e => e.ReserveT).HasColumnName("RESERVE_t");
            entity.Property(e => e.SasS).HasColumnName("SAS_s");
            entity.Property(e => e.SasT).HasColumnName("SAS_t");

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeReadinesses)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeReadiness_SmeKontrola");
        });

        modelBuilder.Entity<SmeSondum>(entity =>
        {
            entity.HasKey(e => e.KontrolaSondaId);

            entity.HasIndex(e => e.KontrolaId, "IX_SmeSonda_Kontrola");

            entity.Property(e => e.KontrolaSondaId).ValueGeneratedNever();
            entity.Property(e => e.AmplitudaHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.AmplitudaMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.AmplitudaMin).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.FrekvenceHodnota).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.FrekvenceMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.FrekvenceMin).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.SignalHodnota1).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.SignalHodnota2).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.SignalMax).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.SignalMin).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Typ)
                .HasMaxLength(10)
                .HasDefaultValue("");
            entity.Property(e => e.Vyusteni)
                .HasMaxLength(10)
                .HasDefaultValue("");

            entity.HasOne(d => d.Kontrola).WithMany(p => p.SmeSonda)
                .HasForeignKey(d => d.KontrolaId)
                .HasConstraintName("FK_SmeSonda_SmeKontrola");
        });

        modelBuilder.Entity<SmeStanice>(entity =>
        {
            entity.HasKey(e => e.Sme);

            entity.ToTable("SmeStanice");

            entity.HasIndex(e => e.Stk, "IX_SmeStanice_STK");

            entity.Property(e => e.Sme)
                .ValueGeneratedNever()
                .HasColumnName("SME");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Jednatel)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.Kategorie).HasMaxLength(100);
            entity.Property(e => e.Kraj).HasMaxLength(50);
            entity.Property(e => e.Majitel)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.Nazev)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Obec).HasMaxLength(50);
            entity.Property(e => e.Orp)
                .HasMaxLength(50)
                .HasColumnName("ORP");
            entity.Property(e => e.Psc)
                .HasMaxLength(10)
                .HasColumnName("PSC");
            entity.Property(e => e.Skupina)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.Stk).HasColumnName("STK");
            entity.Property(e => e.Telefon).HasMaxLength(50);
            entity.Property(e => e.Ulice).HasMaxLength(50);
        });

        modelBuilder.Entity<StkStanice>(entity =>
        {
            entity.HasKey(e => e.Stk);

            entity.ToTable("StkStanice");

            entity.Property(e => e.Stk)
                .ValueGeneratedNever()
                .HasColumnName("STK");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Kod).HasMaxLength(10);
            entity.Property(e => e.Kraj).HasMaxLength(50);
            entity.Property(e => e.Nazev)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Obec).HasMaxLength(50);
            entity.Property(e => e.Orp)
                .HasMaxLength(50)
                .HasColumnName("ORP");
            entity.Property(e => e.Psc)
                .HasMaxLength(10)
                .HasColumnName("PSC");
            entity.Property(e => e.Telefon).HasMaxLength(50);
            entity.Property(e => e.Ulice).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
