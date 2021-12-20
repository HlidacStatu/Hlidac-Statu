using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.99")]
            public static void Init_1_0_0_99(IDatabaseUpgrader du)
            {
                string sql = @"
alter table indoctables add 
	category int,
	hasTerribleFormat bit;

alter table indocjobs add
    Unit int,
    Price decimal(18,9),
    PriceVAT decimal(18,9),
    PriceVATCalculated decimal(18,9),
    VAT decimal(18,9);

	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
