namespace DotaceProcessor.Pipeline.SubsidyHandlers;

public static partial class HandlerFactory
{
    public static readonly Lazy<IHandler> ProjectNameHandler = new(CreateProjectNameHandler);
    public static readonly Lazy<IHandler> ProjectCodeHandler = new(CreateProjectCodeHandler);
    public static readonly Lazy<IHandler> ProgramNameHandler = new(CreateProgramNameHandler);
    public static readonly Lazy<IHandler> ProgramCodeHandler = new(CreateProgramCodeHandler);
    public static readonly Lazy<IHandler> RecipientNameHandler = new(CreateRecipientNameHandler);
    public static readonly Lazy<IHandler> RecipientIcoHandler = new(CreateRecipientIcoHandler);
    public static readonly Lazy<IHandler> RecipientCityHandler = new(CreateRecipientCityHandler);
    public static readonly Lazy<IHandler> RecipientOkresHandler = new(CreateRecipientOkresHandler);
    public static readonly Lazy<IHandler> RecipientPscHandler = new(CreateRecipientPscHandler);

    private static IHandler CreateProjectNameHandler()
    {
        string[] columnNames = ["Název projektu"];
        string handlerName = "ProjectName";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.ProjectName = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: true);
    }
    
    private static IHandler CreateProjectCodeHandler()
    {
        string[] columnNames = ["Číslo"];
        string handlerName = "ProjectCode";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.ProjectCode = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }

    private static IHandler CreateProgramNameHandler()
    {
        string[] columnNames = ["Výzva dotačního titulu"];
        string handlerName = "ProgramName";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.ProgramName = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
    
    private static IHandler CreateProgramCodeHandler()
    {
        string[] columnNames = ["Kód dotačního programu"];
        string handlerName = "ProgramCode";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.ProgramCode = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }

    private static IHandler CreateRecipientNameHandler()
    {
        string[] columnNames = ["Název žadatele"];
        string handlerName = "RecipientName";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.Recipient.Name = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: true);
    }

    private static IHandler CreateRecipientIcoHandler()
    {
        string[] columnNames = ["IČO žadatele"];
        string handlerName = "RecipientIco";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.Recipient.Ico = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }

    private static IHandler CreateRecipientCityHandler()
    {
        string[] columnNames = ["Žadatel obec", "Žadatel_obec"];
        string handlerName = "RecipientCity";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.Recipient.Obec = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
    private static IHandler CreateRecipientOkresHandler()
    {
        string[] columnNames = ["Žadatel okres","Žadatel_okres"];
        string handlerName = "RecipientOkres";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.Recipient.Okres = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
    private static IHandler CreateRecipientPscHandler()
    {
        string[] columnNames = ["Žadatel PSČ"];
        string handlerName = "RecipientPsc";

        void HandlerPropertySetter(PipelineContext context, string columnName, string? value)
        {
            context.Subsidy.Common.Recipient.PSC = value;
        }

        return new ExtractStringValueHandler(columnNames, handlerName, HandlerPropertySetter, isValueMandatory: false);
    }
}