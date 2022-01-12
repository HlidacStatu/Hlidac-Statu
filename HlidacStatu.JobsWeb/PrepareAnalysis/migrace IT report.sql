truncate table ceny
GO
DBCC CHECKIDENT ('[ceny]', RESEED, 0);
GO

INSERT INTO Ceny([JobPK]
           ,[SmlouvaId]
           ,[IcoOdberatele]
           ,[IcoDodavatele]
           ,[TablePk]
           ,[Polozka]
           ,[Tags]
           ,[Unit]
           ,[UnitText]
           ,[PricePerUnit]
           ,[PricePerUnitVAT]
           ,[Year]
           ,[AnalyzaName]
           ,[Created])

SELECT  
	j.pk as JobPk, 
	t.smlouvaID, 
	s.IcoOdberatele, 
	d.Ico as IcoDodavatele, 
	j.tablePK,
	j.jobGrouped as polozka, 
	j.tags,
	2 as unit, --MD from enum MeasureUnit
	'MD' as unitText,
	j.salaryMD as pricePerUnit,
	j.salaryMDVat as pricePerUnitVAT,
	t.year,
    t.subject as AnalyzaName ,
	j.created

FROM InDocTables t
    join InDocJobs j on t.pk = j.tablePK
    join SmlouvyIds s on s.Id = t.smlouvaID
    join SmlouvyDodavatele d on s.Id = d.SmlouvaId
where j.jobGrouped is not null
	and t.subject = 'IT'

	and j.salaryMDVat is not null

