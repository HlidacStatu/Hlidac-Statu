namespace HlidacStatu.Entities.Entities.KIndex
{
    public partial class KIndexData
    {
        public static string KIndexCommentForPart(KIndexParts part, KIndexData.Annual data, bool html = false)
        {
            var txt = data.Info(part).ShortComment;
            if (html)
                return txt;
            else
                return Devmasters.TextUtil.RemoveHTML(txt);
        }

        

        
        

        public bool NotInterestingToShow()
        {
            return false;
        }

        public string SocialInfoBody()
        {
            return ""; //TODO
        }

        public string SocialInfoFooter()
        {
            return ""; //TODO
        }

        public string SocialInfoImageUrl()
        {
            return ""; //TODO
        }

        public string SocialInfoSubTitle()
        {
            return this.LastKIndexLabel().ToString() + " index klíčových rizik";
        }

        public string SocialInfoTitle()
        {
            return this.Jmeno;
        }
    }
}
