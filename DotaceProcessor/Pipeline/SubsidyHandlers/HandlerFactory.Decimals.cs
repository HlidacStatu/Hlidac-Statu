namespace DotaceProcessor.Pipeline.SubsidyHandlers;

public static partial class HandlerFactory
{
    public static readonly Lazy<IHandler> SubsidyAmountHandler = new(CreateSubsidyAmountHandler);
    public static readonly Lazy<IHandler> PayedAmountHandler = new(CreatePayedAmountHandler);
    

    private static IHandler CreateSubsidyAmountHandler()
    {
        string[] columnNames = ["Schválená dotace v Kč"];
        string handlerName = "SubsidyAmount";

        void HandlerPropertySetter(PipelineContext context, string columnName, decimal? value)
        {
            context.Subsidy.Common.SubsidyAmount = value;
        }

        return new ExtractDecimalValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
    
    private static IHandler CreatePayedAmountHandler()
    {
        string[] columnNames = ["Vyplacená dotace v Kč"];
        string handlerName = "PayedAmount";

        void HandlerPropertySetter(PipelineContext context, string columnName, decimal? value)
        {
            context.Subsidy.Common.PayedAmount = value;
        }

        return new ExtractDecimalValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
}