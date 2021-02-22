namespace SonarJsConfig
{
    public interface ITsProtoLogger
    {
        void LogMessage(string message);
        void LogError(string message);

        void LogDebug(string message);
    }

    //public class ConsoleLogger : ITsProtoLogger
    //{
    //    void ITsProtoLogger.LogError(string message)
    //    {
    //        Console.Error.WriteLine(message);
    //    }

    //    void ITsProtoLogger.LogMessage(string message)
    //    {
    //        Console.WriteLine(message);
    //    }

    //    void ITsProtoLogger.LogDebug(string message)
    //    {
    //        Console.WriteLine("DEBUG: " + message);
    //    }
    //}


    public static class SLVSLoggerExtensions
    {
        public static void LogError(this SonarLint.VisualStudio.Integration.ILogger logger, string message)
        {
            logger.WriteLine("ERROR: " + message);
        }


        // alias: just to avoid multiple changes in the prototype code
        public static void LogMessage(this SonarLint.VisualStudio.Integration.ILogger logger, string message) =>
            logger.WriteLine(message);
    }
}
