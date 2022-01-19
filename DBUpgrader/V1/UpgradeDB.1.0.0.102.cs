using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.102")]
            public static void Init_1_0_0_102(IDatabaseUpgrader du)
            {
                string sql = @"
alter table indoctables add
    Klasifikace nvarchar(100) null,
    KlasifikaceManual nvarchar(100) null,
    Analyza nvarchar(100) null;
go
update InDocTables set hasTerribleFormat = null;
go
alter table indoctables drop column hasTerribleFormat;
go
update InDocTables set klasifikace = subject;
update InDocTables set 
	analyza = CASE
				WHEN category = 10000 THEN 'It'
				WHEN category is null and subject is not null then 'It'
				WHEN category is null and subject is null then null
				ELSE 'Jine'
			END,
	klasifikaceManual = CASE
							WHEN category = 10700 THEN 'bezpecnost_obecne'
							WHEN category = 10400 THEN 'telco_obecne'
							WHEN category = 10500 THEN 'zdrav_obecne'
							WHEN category = 10300 THEN 'stroje_obecne'
							WHEN category = 11800 THEN 'marketing_obecne'
							WHEN category = 11900 THEN 'jine_obecne'
							WHEN category = 10200 THEN 'doprava_obecne'
							WHEN category = 10100 THEN 'stav_obecne'
							WHEN category = 11600 THEN 'techsluzby_obecne'
							WHEN category = 11700 THEN 'vyzkum_obecne'
							WHEN category = 10000 THEN 'it_obecne'
							WHEN category = 11500 THEN 'legal_obecne'
							WHEN category = 11400 THEN 'finance_obecne'
							WHEN category = 11300 THEN 'social_obecne'
							WHEN category = 11100 THEN 'kancelar_obecne'
							WHEN category = 11000 THEN 'agro_obecne'
							ELSE null
						END
  where status = 2;

	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
