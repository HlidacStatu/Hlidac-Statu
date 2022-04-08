-- insert Mandays
INSERT INTO Ceny([JobPK]
           ,[SmlouvaId]
           ,[IcoOdberatele]
           ,[IcoDodavatele]
           ,[TablePk]
           ,[Polozka]
           ,[Tags]
           ,[Unit]
           ,[UnitText]
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
    j.PriceVATCalculated as pricePerUnitVAT,
    t.year,
    Upper(t.analyza) as AnalyzaName ,
    j.created

FROM InDocTables t
         join InDocJobs j on t.pk = j.tablePK
         join SmlouvyIds s on s.Id = t.smlouvaID
         join SmlouvyDodavatele d on s.Id = d.SmlouvaId
where j.jobGrouped is not null
  and t.analyza = 'IT'
  and t.year=2021
  and j.PriceVATCalculated is not null
  and (j.pk not in (select jobpk from Ceny))
  and Unit = 2 --manday


-- insert hours translated into mandays
INSERT INTO Ceny([JobPK]
    ,[SmlouvaId]
    ,[IcoOdberatele]
    ,[IcoDodavatele]
    ,[TablePk]
    ,[Polozka]
    ,[Tags]
    ,[Unit]
    ,[UnitText]
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
    j.PriceVATCalculated * 8 as pricePerUnitVAT,
    t.year,
    Upper(t.analyza) as AnalyzaName ,
    j.created

FROM InDocTables t
         join InDocJobs j on t.pk = j.tablePK
         join SmlouvyIds s on s.Id = t.smlouvaID
         join SmlouvyDodavatele d on s.Id = d.SmlouvaId
where j.jobGrouped is not null
  and t.analyza = 'IT'
  and t.year=2021
  and j.PriceVATCalculated is not null
  and (j.pk not in (select jobpk from Ceny))
  and Unit = 1 --hour
