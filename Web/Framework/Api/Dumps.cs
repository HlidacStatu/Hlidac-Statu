﻿using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;

using static HlidacStatu.Web.Models.ApiV1Models;

namespace HlidacStatu.Web.Framework.Api
{
    public class Dumps
    {


        public static Models.ApiV1Models.DumpInfoModel[] GetDumps(string baseUrl = "https://api.hlidacstatu.cz/api/v2/")
        {

            List<DumpInfoModel> data = new List<DumpInfoModel>();

            foreach (var fi in new System.IO.DirectoryInfo(StaticData.Dumps_Path).GetFiles("*.zip"))
            {
                var fn = fi.Name;
                var regexStr = @"((?<type>(\w*))? \.)? (?<name>(\w|-)*)\.dump -? (?<date>\d{4} - \d{2} - \d{2})?.zip";
                DateTime? date = Devmasters.DT.Util.ToDateTimeFromCode(Devmasters.RegexUtil.GetRegexGroupValue(fn, regexStr, "date"));
                string name = Devmasters.RegexUtil.GetRegexGroupValue(fn, regexStr, "name");
                string dtype = Devmasters.RegexUtil.GetRegexGroupValue(fn, regexStr, "type");
                if (!string.IsNullOrEmpty(dtype))
                    name = dtype + "." + name;
                data.Add(
                    new DumpInfoModel()
                    {
                        url = baseUrl + $"dump/{name}/{date?.ToString("yyyy-MM-dd") ?? ""}",
                        created = fi.LastWriteTimeUtc,
                        date = date,
                        fulldump = date.HasValue == false,
                        size = fi.Length,
                        dataType = name
                    }
                    ); ;
            }



            return data.ToArray();
        }


    }
}