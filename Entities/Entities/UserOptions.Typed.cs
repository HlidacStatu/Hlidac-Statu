using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities
{

    public abstract class UserOptions<T>
        : UserOptions
    {

        public UserOptions()
        { }

        public UserOptions(ParameterType option, int? languageId = null)
            :base(option,languageId)
        {
        }

        public UserOptions(ApplicationUser user, ParameterType option, int? languageId = null)
        {
            var uo = Get(user.Id, option, languageId);
            LanguageId = languageId;
            UserId = user.Id;
            OptionId = (int)option;
            if (uo == null)
                Value = null;
            else
            Value = uo.GetValue();
        }

        public new virtual T GetValue()
        {
            return DeserializeFromString(base.GetValue());
        }
        public virtual void SetValue(T value)
        {
            base.SetValue(SerializeToString(value));
        }

        protected abstract string SerializeToString(T value);
        protected abstract T DeserializeFromString(string value);

        public new virtual void Remove()
        {
            //migrace: nenašel jsem, kde se používá
            using (var db = new DbEntities())
            {
                db.Database.ExecuteSqlRaw($@"EXEC UserOption_Add @optionId = {OptionId}, @userid = {UserId}, @languageid = {LanguageId};");
            }
        }

        //public virtual static UserOptions<T> Create(ParameterType option, T value, ApplicationUser customer = null)
        //{
        //    UserOptions o = new UserOptions(customer, option);
        //    o.SetValue(value);
        //    o.Save();
        //    return o;
        //}




    }
}
