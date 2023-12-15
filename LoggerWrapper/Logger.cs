using System.Net.Mail;
using Serilog;

namespace LoggerWrapper;

public class Logger
{
    public void Error(string messageTemplate, Exception exception, params object?[]? parameters)
    {
        Log.Error(exception, messageTemplate, parameters);
    }
    
    public void Error(string messageTemplate, params object?[]? parameters)
    {
        Log.Error(messageTemplate, parameters);
    }

    public void Info(string messageTemplate, Exception exception, params object?[]? parameters)
    {
        Log.Information(exception, messageTemplate, parameters);
    }
    public void Info(string messageTemplate, params object?[]? parameters)
    {
        Log.Information(messageTemplate, parameters);
    }
    
    public void Debug(string messageTemplate, Exception exception, params object?[]? parameters)
    {
        Log.Debug(exception, messageTemplate, parameters);
    }
    public void Debug(string messageTemplate, params object?[]? parameters)
    {
        Log.Debug(messageTemplate, parameters);
    }
    
    public void Warning(string messageTemplate, Exception exception, params object?[]? parameters)
    {
        Log.Warning(exception, messageTemplate, parameters);
    }
    public void Warning(string messageTemplate, params object?[]? parameters)
    {
        Log.Warning(messageTemplate, parameters);
    }
    
    
    
}