using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HlidacStatu.Util
{
    public class SmlouvaSplitter
    {
        public interface ISplitRender
        {
            string ToText();
            string ToHtml();
        }
        public class SplitSmlouva : ISplitRender
        {
            public class Part : ISplitRender
            {
                public int StartingPosition { get; set; } = 0;
                public int Length { get; set; } = 0;
                public int Order { get; set; } = 0;
                public string Text { get; set; } = "";

                public virtual string ToHtml()
                {
                    return Text.Replace("\n", "<br />");
                }

                public virtual string ToText()
                {
                    return Text;
                }
            }

            public class Paragraph : Part, ISplitRender
            {
                //public Part ParagraphPart { get; set; } = new Part();
                public List<Part> Sentences { get; set; } = new List<Part>();

                public new string ToHtml()
                {
                    return string.Join(" ", Sentences.Select(s => s.ToHtml()).ToArray());
                }
                public new string ToText()
                {
                    return string.Join(" ", Sentences.Select(s => s.ToText()).ToArray());
                }
            }

            public string SmlouvaId { get; set; }
            public string PrilohaId { get; set; }
            public string SectionName { get; set; }
            public List<Paragraph> Paragraphs { get; set; } = new List<Paragraph>();


            public string ToText()
            {
                return string.Join("\n\n", Paragraphs.Select(p => p.ToText()).ToArray());
            }

            public string ToHtml()
            {
                return string.Join("<br /><br />", Paragraphs.Select(p => p.ToText()).ToArray());
            }


            /*
             var paragraphs = Regex.Split(fileText, @\"(\\r\\n?|\\n){2}\")
    .Where(p => p.Any(char.IsLetterOrDigit));

string[] sentences = Regex.Split(input, @"(?<=[\.!\?])\p+");
 
             
             */

            static RegexOptions options = (
                RegexOptions.Compiled
                | RegexOptions.IgnorePatternWhitespace
                | RegexOptions.Multiline
                //| RegexOptions.IgnoreCase
                );

            static string paragraphRegexStr = @"(\r\n?\r\n?|\n\n)";
            static Regex paragraphsRegex = new Regex(paragraphRegexStr, options);
            static string sentencesRegexStr = @"([\.!\?,:;] \s* (\n|\r|$) )";
            static Regex sentencesRegex = new Regex(sentencesRegexStr, options);
            public static SplitSmlouva ParseText(string text)
            {
                int posInStr = 0;
                int order = 0;

                SplitSmlouva ss = new SplitSmlouva();

                var parMatches = paragraphsRegex.Matches(text);
                if (parMatches?.Count > 0)
                {
                    foreach (Match m in parMatches)
                    {
                        var foundParagr = text.Substring(posInStr, m.Index - posInStr);
                        if (foundParagr.Any(char.IsLetterOrDigit) == false)
                            continue;

                        Paragraph p = ParseParagraph(text, ref posInStr, ref order, m.Length, foundParagr);
                        ss.Paragraphs.Add(p);
                    }//per parMatches

                    var lastPar = ss.Paragraphs.Last();
                    var endOfFile = text.Length;;
                    var endOfLastSent = lastPar.StartingPosition + lastPar.Length;
                    if (endOfFile - endOfLastSent > 1)
                    {
                        var lastParagr = text.Substring(endOfLastSent);
                        Paragraph p = ParseParagraph(text, ref endOfLastSent, ref order, 0, lastParagr);
                        ss.Paragraphs.Add(p);
                    }

                } //parMatches

                return ss;
            }

            private static Paragraph ParseParagraph(string text, ref int posInStr, ref int order, int matchLength, string foundParagr)
            {
                Paragraph p = new Paragraph();
                p.StartingPosition = posInStr;
                p.Length = foundParagr.Length;
                p.Text = foundParagr;
                p.Order = order;
                order++;
                posInStr = posInStr + foundParagr.Length + matchLength;

                var senMatches = sentencesRegex.Matches(p.Text);
                if (senMatches?.Count > 0)
                {
                    int sorder = 0;
                    int posSentInStr = 0;
                    foreach (Match sm in senMatches)
                    {
                        var foundSent = p.Text.Substring(posSentInStr, (sm.Index + sm.Length) - posSentInStr);
                        if (foundSent.Any(char.IsLetterOrDigit) == false)
                            continue;

                        Part sent = new Part();
                        sent.StartingPosition = posSentInStr;
                        sent.Length = foundSent.Length;
                        sent.Text = foundSent;
                        sent.Order = sorder;
                        sorder++;
                        p.Sentences.Add(sent);
                        posSentInStr = posSentInStr + foundSent.Length; // + sm.Value.Length;
                    }
                    var endOfParag = p.StartingPosition + p.Length;
                    var endOfLastSent = p.StartingPosition
                        + p.Sentences.Last().StartingPosition + p.Sentences.Last().Length;
                    if (endOfParag - endOfLastSent > 1)
                    {
                        //text.Substring(endOfLastSent, endOfParag-endOfLastSent)
                        Part sent = new Part();
                        sent.StartingPosition = endOfLastSent;
                        sent.Length = endOfParag - endOfLastSent;
                        sent.Text = text.Substring(endOfLastSent, endOfParag - endOfLastSent);
                        sent.Order = sorder;
                        p.Sentences.Add(sent);
                    }
                }
                else
                {
                    p.Sentences.Add((Part)p);
                }

                return p;
            }
        }
    }
}