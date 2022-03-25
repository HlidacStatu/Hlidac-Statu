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
	iif(j.tags='',null, j.tags) as tags,
	2 as unit, --MD from enum MeasureUnit
	'MD' as unitText,
	iif(j.UnitCount>1, j.salaryMD/j.unitcount,j.salaryMD) as pricePerUnit,
	iif(j.UnitCount>1, j.salaryMDVat/j.unitcount,j.salaryMDVat)as pricePerUnitVAT,
	t.year,
    Upper(t.analyza) as AnalyzaName ,
	j.created

FROM InDocTables t
    join InDocJobs j on t.pk = j.tablePK
    join SmlouvyIds s on s.Id = t.smlouvaID
    join SmlouvyDodavatele d on s.Id = d.SmlouvaId
where j.jobGrouped is not null
	and t.analyza = 'IT'
	and t.year=2020
	and j.salaryMDVat is not null
	and (j.pk not in (select jobpk from Ceny))




