using System.Text;
using System.Threading.Tasks;
using Foundatio.Parsers.LuceneQueries.Extensions;
using Foundatio.Parsers.LuceneQueries.Nodes;
using Foundatio.Parsers.LuceneQueries.Visitors;

namespace HlidacStatu.Web.Views.Shared.Components;

public class HQueryNodeVisitor: QueryNodeVisitorWithResultBase<string>
{
    private readonly StringBuilder _builder = new("Vyhledat");
    
    private HQueryNodeVisitor()
    {
        
    }
    
    // spouštět tímhle
    public static Task<string> RunAsync(IQueryNode node) {
        return new HQueryNodeVisitor().AcceptAsync(node, null);
    }
    
    public override async Task<string> AcceptAsync(IQueryNode node, IQueryVisitorContext context) {
        await node.AcceptAsync(this, context);
        return _builder.ToString();
    }
    
    public override async Task VisitAsync(GroupNode node, IQueryVisitorContext context) {
        
        if (node.IsNegated())
            _builder.Append(" kde neplatí (");
        if (node.Operator == GroupOperator.Or)
            _builder.Append(" (");
        

        if (node.Left != null)
            await node.Left.AcceptAsync(this, context);

        if (node.Operator == GroupOperator.Or)
        {
            _builder.Append(") NEBO (");
        }

        if (node.Right != null)
            await node.Right.AcceptAsync(this, context);


        if (node.IsNegated() || node.Operator == GroupOperator.Or)
        {
            _builder.Append(" )");
        }
    }

    public override void Visit(TermNode node, IQueryVisitorContext context)
    {
        if (node.IsQuotedTerm)
        {
            _builder.Append($" přesný výraz \"{node.Term}\"");
        }
        else
        {
            _builder.Append($" {node.Term}");
            
        }
    }

    private string IsInclusiveText(bool? isInclusive) => isInclusive == true ? " (včetně)" : "";
    private string IsNegatedText(bool? isNegated) => isNegated == true ? "ne" : "";


    public override void Visit(TermRangeNode node, IQueryVisitorContext context) 
    {
        _builder.Append($" v období od {node.Min}{IsInclusiveText(node.MinInclusive)} do {node.Max}{IsInclusiveText(node.MinInclusive)}");
    }

    public override void Visit(ExistsNode node, IQueryVisitorContext context)
    {
        _builder.Append($" kde {IsNegatedText(node.IsNegated)}existuje pole {node.Field}");
    }

    public override void Visit(MissingNode node, IQueryVisitorContext context) {
        _builder.Append($" kde {IsNegatedText(node.IsNegated)}chybí pole {node.Field}");
    }
    
}