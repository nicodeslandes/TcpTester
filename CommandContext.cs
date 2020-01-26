using System.IO;

namespace TcpTester
{
    public class CommandContext
    {
        public CommandContext(CommandRegistry commandRegistry)
        {
            CommandRegistry = commandRegistry;
        }
        public Stream? ConnectionStream { get; set; }
        public CommandRegistry CommandRegistry { get; }
        public int DataLength { get; set; } = 64;
    }


}
