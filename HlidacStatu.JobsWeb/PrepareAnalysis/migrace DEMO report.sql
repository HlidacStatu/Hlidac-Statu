
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
    'DEMO' as AnalyzaName ,
	j.created


FROM InDocTables t
    join InDocJobs j on t.pk = j.tablePK
    join SmlouvyIds s on s.Id = t.smlouvaID
    join SmlouvyDodavatele d on s.Id = d.SmlouvaId
where 
	(t.subject = 'DEMO' or category between 10000 and 10099)
	--(t.subject = 'it' and category between 10000 and 10099)

	and t.year=2018
	and j.jobGrouped is not null and j.jobGrouped != '0'
	and j.salaryMDVat is not null
	and (j.pk not in (select jobpk from Ceny))


