using System.Xml.Serialization;
using System.Collections.Generic;

namespace HlidacStatu.Entities.Entities.TextyHlidacStatu
{
  
    [XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
    public class Link
    {
        [XmlAttribute(AttributeName = "href")] public string Href { get; set; }
        [XmlAttribute(AttributeName = "rel")] public string Rel { get; set; }
        [XmlAttribute(AttributeName = "type")] public string Type { get; set; }
    }

    [XmlRoot(ElementName = "image")]
    public class Image
    {
        [XmlElement(ElementName = "url")] public string Url { get; set; }
        [XmlElement(ElementName = "title")] public string Title { get; set; }
        [XmlElement(ElementName = "link")] public string Link2 { get; set; }
        [XmlElement(ElementName = "width")] public string Width { get; set; }
        [XmlElement(ElementName = "height")] public string Height { get; set; }
    }

    [XmlRoot(ElementName = "site", Namespace = "com-wordpress:feed-additions:1")]
    public class Site
    {
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }

        [XmlText] public string Text { get; set; }
    }

    [XmlRoot(ElementName = "guid")]
    public class Guid
    {
        [XmlAttribute(AttributeName = "isPermaLink")]
        public string IsPermaLink { get; set; }

        [XmlText] public string Text { get; set; }
    }

    [XmlRoot(ElementName = "post-id", Namespace = "com-wordpress:feed-additions:1")]
    public class Postid
    {
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }

        [XmlText] public string Text { get; set; }
    }

    [XmlRoot(ElementName = "item")]
    public class Item
    {
        [XmlElement(ElementName = "title")] public string Title { get; set; }
        [XmlElement(ElementName = "link")] public string Link2 { get; set; }
        [XmlElement(ElementName = "comments")] public List<string> Comments { get; set; }

        [XmlElement(ElementName = "creator", Namespace = "http://purl.org/dc/elements/1.1/")]
        public string Creator { get; set; }

        [XmlElement(ElementName = "pubDate")] public string PubDate { get; set; }
        [XmlElement(ElementName = "category")] public List<string> Categories { get; set; }
        [XmlElement(ElementName = "guid")] public Guid Guid { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "commentRss", Namespace = "http://wellformedweb.org/CommentAPI/")]
        public string CommentRss { get; set; }

        [XmlElement(ElementName = "name", Namespace = "https://publishpress.com/")]
        public string Name { get; set; }

        [XmlElement(ElementName = "post-id", Namespace = "com-wordpress:feed-additions:1")]
        public Postid Postid { get; set; }
    }

    [XmlRoot(ElementName = "channel")]
    public class Channel
    {
        [XmlElement(ElementName = "title")] public string Title { get; set; }

        [XmlElement(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
        public Link Link { get; set; }

        [XmlElement(ElementName = "link")] public string Link2 { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "lastBuildDate")]
        public string LastBuildDate { get; set; }

        [XmlElement(ElementName = "language")] public string Language { get; set; }

        [XmlElement(ElementName = "updatePeriod", Namespace = "http://purl.org/rss/1.0/modules/syndication/")]
        public string UpdatePeriod { get; set; }

        [XmlElement(ElementName = "updateFrequency", Namespace = "http://purl.org/rss/1.0/modules/syndication/")]
        public string UpdateFrequency { get; set; }

        [XmlElement(ElementName = "generator")]
        public string Generator { get; set; }

        [XmlElement(ElementName = "image")] public Image Image { get; set; }

        [XmlElement(ElementName = "site", Namespace = "com-wordpress:feed-additions:1")]
        public Site Site { get; set; }

        [XmlElement(ElementName = "item")] public List<Item> Items { get; set; }
    }

    [XmlRoot(ElementName = "rss")]
    public class TextyRss
    {
        [XmlElement(ElementName = "channel")] public Channel Channel { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }

        [XmlAttribute(AttributeName = "content", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Content { get; set; }

        [XmlAttribute(AttributeName = "wfw", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Wfw { get; set; }

        [XmlAttribute(AttributeName = "dc", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Dc { get; set; }

        [XmlAttribute(AttributeName = "atom", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Atom { get; set; }

        [XmlAttribute(AttributeName = "sy", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Sy { get; set; }

        [XmlAttribute(AttributeName = "slash", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Slash { get; set; }

        [XmlAttribute(AttributeName = "series", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Series { get; set; }

        [XmlAttribute(AttributeName = "georss", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Georss { get; set; }

        [XmlAttribute(AttributeName = "geo", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Geo { get; set; }
    }
}