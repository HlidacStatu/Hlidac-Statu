using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HlidacStatu.Entities
{

    public partial class UserOptions
    {
        //migrace: rozdělit ještě na repozitory část, kde se pracuje s DB
        public enum ParameterType
        {
            DatLabLastUpdate = 1,
            WatchdogAllInOne = 2,
        }

        [NotMapped]
        public ParameterType Parameter
        {
            get
            {
                return (ParameterType)OptionId;
            }
            set
            {
                OptionId = (int)value;
            }
        }

        public UserOptions()
        {
        }

        protected UserOptions(ParameterType option, int? languageId = null)
        {
            LanguageId = languageId;
            UserId = null;
            OptionId = (int)option;

            using (var db = new DbEntities())
            {
                var r = db.UserOptions
                    .FromSqlInterpolated($@"EXEC UserOption_Get @optionId = {(int)option}, @userid = {null}, @languageid = {languageId}")
                    .FirstOrDefault();

                Created = r?.Created ?? DateTime.Now;
                Value = r?.Value;
            }

            // var r = new DbEntities().UserOption_Get((int)option, null, LanguageId)
            //     .FirstOrDefault();
        }

        protected UserOptions(ApplicationUser user, ParameterType option, int? languageId = null)
        {
            LanguageId = languageId;
            UserId = user.Id;
            OptionId = (int)option;
        }

        public virtual void Save()
        {
            using (var db = new DbEntities())
            {
                db.Database.ExecuteSqlInterpolated($@"EXEC UserOption_Add @optionId = {OptionId}, @userid = {UserId}, @value = {Value}, @languageid = {LanguageId};");
            }
        }

        public virtual string GetValue()
        {
            return Value;
        }
        public virtual void SetValue(string value)
        {
            Value = value;
        }

        public virtual void Remove()
        {
            //migrace: nenašel jsem kde se používá
            using (var db = new DbEntities())
            {
                db.Database.ExecuteSqlInterpolated($@"EXEC UserOption_Add @optionId = {OptionId}, @userid = {UserId}, @languageid = {LanguageId};");
            }
        }

        protected static UserOptions Get(string userId, ParameterType option, int? languageid)
        {
            using (var db = new DbEntities())
            {
                var r = db.UserOptions
                    .FromSqlInterpolated(
                        $@"EXEC UserOption_Get @optionId = {(int)option}, @userid = {userId}, @languageid = {languageid}")
                    .FirstOrDefault();
                return r;
            }

        }

    }
    public partial class UserOption_Get_Result
    {
        public static explicit operator UserOptions(UserOption_Get_Result res)
        {
            if (res == null)
                return null;
            var uo = new UserOptions()
            {
                Pk = res.pk,
                Created = res.Created,
                LanguageId = res.LanguageId,
                OptionId = res.OptionId,
                UserId = res.UserId
            };
            uo.SetValue(res.Value);
            return uo;
        }
    }
}
