using Serilog;

namespace Rogero.WpfNavigation;

public static class InternalLogger
{
    public static ILogger LoggerInstance { get; set; }

    public static void Information(string messageTemplate)
    {
        LoggerInstance?.Information(messageTemplate);
    }

    public static void Information<T>(string messageTemplate, T propertyValue)
    {
        LoggerInstance?.Information(messageTemplate, propertyValue);
    }

    public static void Information<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        LoggerInstance?.Information(messageTemplate, propertyValue0, propertyValue1);
    }

    public static void Information<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        LoggerInstance?.Information(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    public static void Warning(string messageTemplate)
    {
        LoggerInstance?.Warning(messageTemplate);
    }

    public static void Warning<T>(string messageTemplate, T propertyValue)
    {
        LoggerInstance?.Warning(messageTemplate, propertyValue);
    }

    public static void Warning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        LoggerInstance?.Warning(messageTemplate, propertyValue0, propertyValue1);
    }

    public static void Warning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        LoggerInstance?.Warning(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    public static void Warning(string messageTemplate, params object[] propertyValues)
    {
        LoggerInstance?.Warning(messageTemplate, propertyValues);
    }
}