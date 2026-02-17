using HlidacStatu.Repositories.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class Graph
    {
        public static class Render
        {
            public static async Task<string> RenderVazbaAsync(HlidacStatu.DS.Graphs.Graph.Edge vazbaToRender,
                bool html = true)
            {
                if ( vazbaToRender == null)
                {
                    return string.Empty;
                }

                var firstFromName = await PrintNameAsync(vazbaToRender.From, html);
                var firstToName = await PrintNameAsync(vazbaToRender.To, html);

                var ret = html ? 
                    $"{firstFromName} <i>/{vazbaToRender.Descr}/</i> v {firstToName} {vazbaToRender.Doba()}"
                    : $"{firstFromName} /{vazbaToRender.Descr}/ v {firstToName} {vazbaToRender.Doba()}"; 

                return ret;

            }


            public static async Task<string> RenderVazbyAsync(HlidacStatu.DS.Graphs.Graph.Edge[] vazbyToRender,
                bool html = true, bool multiline = true)
            {

                if (vazbyToRender == null)
                {
                    return string.Empty;
                }
                if (!vazbyToRender.Any())
                {
                    return string.Empty;
                }
 
                var firstVazba = vazbyToRender.First();

                if (vazbyToRender.Count() == 1)
                {
                    return await RenderVazbaAsync(firstVazba, html);
                }
                else
                { 
                    var firstFromName = await PrintNameAsync(firstVazba.From, html);
                    var firstToName = await PrintNameAsync(firstVazba.To, html);

                    var remainingNames = new List<string>();
                    foreach (var m in vazbyToRender.Skip(1))
                    {
                        remainingNames.Add($"<i>{m.Descr}</i> v {await PrintNameAsync(m.To, html)}");
                    }

                    var ret = html ? "Nepřímá vazba přes:" + (multiline ? "<br />" : "")
                                    + "<small>"
                                    + $"{firstFromName} <i>/{firstVazba.Descr}/</i> v {firstToName} {firstVazba.Doba()}"
                                    + $" → "
                                    + string.Join(" → ", remainingNames)
                                    + "</small>"
                              : "Nepřímá vazba přes:" + (multiline ? "<br />" : "")
                                    + $"{firstFromName} /{firstVazba.Descr}/ v {firstToName} {firstVazba.Doba()}"
                                    + $" → "
                                    + string.Join(" → ", remainingNames)

                                    ;
                    return ret;
                }
            }
            public static async Task<string> RenderPathAsync(
                IEnumerable<PlneVazby.AllPathsFinder.PathResult> pathToRender,
            bool html = true, bool multiline = true, bool onlyFirst = true)
            {

                if (pathToRender == null)
                {
                    return string.Empty;
                }
                if (!pathToRender.Any())
                {
                    return string.Empty;
                }

                if (onlyFirst == false)
                    throw new NotImplementedException("Rendering of multiple paths is not implemented yet.");

                var path = pathToRender.First();

                var firstVazba = path.Edges.First().BindingPayload;

                if (path.Edges.Count() == 1)
                {
                    return await RenderVazbaAsync(firstVazba, html);
                }
                else
                {
                    var firstFromName = await PrintNameAsync(firstVazba.From, html);
                    var firstToName = await PrintNameAsync(firstVazba.To, html);

                    var remainingNames = new List<string>();
                    foreach (var m in path.Edges.Skip(1))
                    {
                        remainingNames.Add($"<i>{m.BindingPayload.Descr}</i> v {await PrintNameAsync(m.BindingPayload.To, html)}");
                    }

                    var ret = html ? "Nepřímá vazba přes:" + (multiline ? "<br />" : "")
                                    + "<small>"
                                    + $"{firstFromName} <i>/{firstVazba.Descr}/</i> v {firstToName} {firstVazba.Doba()}"
                                    + $" → "
                                    + string.Join(" → ", remainingNames)
                                    + "</small>"
                              : "Nepřímá vazba přes:" + (multiline ? "<br />" : "")
                                    + $"{firstFromName} /{firstVazba.Descr}/ v {firstToName} {firstVazba.Doba()}"
                                    + $" → "
                                    + string.Join(" → ", remainingNames)

                                    ;
                    return ret;
                }
            }
            public static async Task<string> PrintNameAsync(HlidacStatu.DS.Graphs.Graph.Node node, bool html = false)
            {
                switch (node.Type)
                {
                    case HlidacStatu.DS.Graphs.Graph.Node.NodeType.Person:
                        return (await OsobaCache.GetPersonByIdAsync(Convert.ToInt32(node.Id)))?.FullNameWithYear(html) ?? "(neznámá osoba)";
                    case HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company:
                    default:
                        return await FirmaCache.GetJmenoAsync(node.Id);
                }
            }
        }
    }
}