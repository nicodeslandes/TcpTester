using System;
using System.Net;
using System.Net.Sockets;

namespace TcpTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var commandRegistry = RegisterCommands();
            var context = new CommandContext(commandRegistry);

            for (int i = 0; i < args.Length; i++)
            {
                var command = ParseCommandFromLaunchArguments(commandRegistry, args, ref i);
                command(context);
            }

            Action<CommandContext> currentCommand = _ => { };
            while (true)
            {
                try
                {
                    Console.Write("> ");
                    var line = Console.ReadLine();
                    if (line == null
                        || line.Equals("q", StringComparison.InvariantCultureIgnoreCase)
                        || line.Equals("quit", StringComparison.InvariantCultureIgnoreCase)
                        || line.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }

                    line = line.Trim();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        currentCommand(context);
                        continue;
                    }

                    currentCommand = ParseCommandFromCli(commandRegistry, line);
                    currentCommand(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            Console.WriteLine("Bye");
        }

        public static CommandRegistry RegisterCommands()
        {
            var registry = new CommandRegistry();
            registry.RegisterCommand<DnsEndPoint>("c", "connect", "Connect to a remote server", (target, ctx) =>
             {
                 Console.WriteLine($"Connecting to {target.Host}:{target.Port}");
                 var tcpClient = new TcpClient(target.Host, target.Port);
                 ctx.ConnectionStream = tcpClient.GetStream();
             });

            registry.RegisterCommand("h", "help", "Display help", ctx =>
            {
                Console.WriteLine("Available commands:");
            });

            registry.RegisterCommand("s", "send", "Send bytes", ctx =>
            {
                if (ctx.ConnectionStream == null)
                {
                    Console.WriteLine("Connect first");
                }
                else
                {
                    var bytes = new byte[ctx.DataLength];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] = (byte)(i & 0xFF);
                    }

                    Console.WriteLine($"Sending {bytes.Length:N0} bytes");
                    ctx.ConnectionStream.Write(bytes);
                }
            });

            registry.RegisterCommand<int>("l", "length", "Set bytes count", (count, ctx) => ctx.DataLength = count);

            return registry;
        }

        public static Action<CommandContext> ParseCommandFromLaunchArguments(CommandRegistry commandRegistry, string[] args, ref int index)
        {
            var i = index;
            string? GetNextArgument() => i < args.Length ? args[i++] : null;

            var commandName = GetNextArgument();
            if (commandName == null) throw new Exception("Missing command name");

            if (!commandName.StartsWith("-"))
            {
                throw new Exception($"Unknow option: {commandName}");
            }
            commandName = commandName.Substring(1);

            var command = commandRegistry.ParseCommand(commandName, GetNextArgument);

            index = i;
            return command;
        }

        public static Action<CommandContext> ParseCommandFromCli(CommandRegistry commandRegistry, string line)
        {
            var commandElements = line.Split(" ");
            int i = 0;
            string? GetNextArgument() => i < commandElements.Length ? commandElements[i++] : null;

            var commandName = GetNextArgument();
            if (commandName == null) throw new Exception("Missing command name");

            return commandRegistry.ParseCommand(commandName, GetNextArgument);
        }
    }
}
