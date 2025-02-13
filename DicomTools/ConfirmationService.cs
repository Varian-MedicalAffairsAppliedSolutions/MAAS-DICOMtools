using System.CommandLine;
using System.CommandLine.IO;

namespace DicomTools
{
    public class ConfirmationService(IConsole m_console) : IConfirmationService
    {
        public bool Confirm(string question)
        {
            m_console.Out.Write(question);
            // IConsole does not support input.
            var yesNo = Console.ReadKey(true);
            m_console.Out.WriteLine();
            return yesNo.Key == ConsoleKey.Y;
        }
    }
}
