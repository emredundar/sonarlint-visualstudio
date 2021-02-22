using System;
using SonarLint.VisualStudio.Integration;

namespace SonarJSPocTests
{
    internal class ConsoleLogger : ILogger
    {
        void ILogger.WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        void ILogger.WriteLine(string messageFormat, params object[] args)
        {
            var message = string.Format(messageFormat, args);
            Console.WriteLine(message);
        }
    }
}
