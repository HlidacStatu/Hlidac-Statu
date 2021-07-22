namespace HlidacStatu.Entities.Entities.Analysis
{
    public class BasicData
    {
        public static BasicData Empty() { return new BasicData(); }
        public BasicData() { }
        public BasicData(BasicData bd)
        {
            Pocet = bd.Pocet;
            CelkemCena = bd.CelkemCena;
        }
        public BasicData(BasicDataPerYear bdy)
        {
            Pocet = bdy.Summary.Pocet;
            CelkemCena = bdy.Summary.CelkemCena;
        }

        public long Pocet { get; set; } = 0;
        public decimal CelkemCena { get; set; } = 0;

        public void Add(long pocet, decimal cena)
        {
            Pocet = Pocet + pocet;
            CelkemCena = CelkemCena + cena;
        }

        public string ToNiceString(IBookmarkable item, bool html = true, string customUrl = null, bool twoLines = false)
        {

            if (html)
            {
                var s = "<a href='" + (customUrl ?? (item?.GetUrl(false) ?? "")) + "'>" +
                            Devmasters.Lang.Plural.Get(Pocet, "{0} smlouva;{0} smlouvy;{0} smluv") +
                        "</a>" + (twoLines ? "<br />" : " za ") +
                        "celkem " +
                        Smlouva.NicePrice(CelkemCena, html: true, shortFormat: true);
                return s;
            }
            else
                return Devmasters.Lang.Plural.Get(Pocet, "{0} smlouva;{0} smlouvy;{0} smluv") +
                    " za celkem " + Smlouva.NicePrice(CelkemCena, html: false, shortFormat: true);

        }
    }

    public class BasicData<T> : BasicData
    {
        public BasicData()
        {
        }

        public BasicData(T data)
        {
            Item = data;
        }
        public BasicData(T data, BasicDataPerYear bdy) : base(bdy)
        {
            Item = data;
        }

        public BasicData(T data, BasicData bd) : base(bd)
        {
            Item = data;
        }

        public T Item { get; set; } = default(T);
    }


    public class BasicDataForSubject<T> : BasicData<string>
        where T : new()
    {
        public T Detail { get; set; } = new T();

        public string Ico
        {
            get { return Item; }
            set { Item = value; }
        }
    }

}
