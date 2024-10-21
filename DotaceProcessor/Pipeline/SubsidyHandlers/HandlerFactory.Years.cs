namespace DotaceProcessor.Pipeline.SubsidyHandlers;

public static partial class HandlerFactory
{
    public static readonly Lazy<IHandler> ApprovedYearHandler = new(CreateApprovedYearHandler);
    public static readonly Lazy<IHandler> YearOfBirthHandler = new(CreateYearOfBirthHandler);
    

    private static IHandler CreateApprovedYearHandler()
    {
        string[] columnNames = ["Datum schválení žádosti", "Datum schválení"];
        string handlerName = "ApprovedYear";

        void HandlerPropertySetter(PipelineContext context, string columnName, int? value)
        {
            context.Subsidy.Common.ApprovedYear = value;
        }

        return new ExtractYearValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
    
    private static IHandler CreateYearOfBirthHandler()
    {
        string[] columnNames = ["Rok narození žadatele"];
        string handlerName = "YearOfBirth";

        void HandlerPropertySetter(PipelineContext context, string columnName, int? value)
        {
            context.Subsidy.Common.Recipient.YearOfBirth = value;
        }

        return new ExtractYearValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
}