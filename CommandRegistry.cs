using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace TcpTester
{
    public class CommandRegistry
    {
        private Dictionary<string, CommandDefinitionBase> _commandDefinitionsByShortName = new Dictionary<string, CommandDefinitionBase>();
        private Dictionary<string, CommandDefinitionBase> _commandDefinitionsByLongName = new Dictionary<string, CommandDefinitionBase>();

        public void RegisterCommand(string shortName, string longName, string description, Action<CommandContext> execute)
        {
            var definition = new CommandDefinition(description, execute);
            _commandDefinitionsByShortName.Add(shortName, definition);
            _commandDefinitionsByLongName.Add(longName, definition);
        }

        public void RegisterCommand<T>(string shortName, string longName, string description, Action<T, CommandContext> execute)
        {
            var definition = new CommandDefinition<T>(description, execute);
            _commandDefinitionsByShortName.Add(shortName, definition);
            _commandDefinitionsByLongName.Add(longName, definition);
        }

        public Action<CommandContext> ParseCommand(string name, Func<string?> argumentProvider)
        {
            if (!_commandDefinitionsByShortName.TryGetValue(name, out var commandDefinition)
                && !_commandDefinitionsByLongName.TryGetValue(name, out commandDefinition))
            {
                throw new InvalidDataException($"Unknown command: {name}");
            }

            return commandDefinition.CreateCommand(argumentProvider);
        }

        abstract class CommandDefinitionBase
        {
            public string Description { get; }
            public abstract Action<CommandContext> CreateCommand(Func<string?> argumentProvider);

            protected CommandDefinitionBase(string description)
            {
                Description = description;
            }

            protected T ExtractArgument<T>(Func<string?> argumentProvider)
            {
                var argumentString = argumentProvider();
                if (argumentString == null)
                {
                    throw new Exception("Missing argument");
                }

                if (typeof(T) == typeof(DnsEndPoint))
                {
                    var elements = argumentString.Split(":");
                    if (elements.Length != 2) throw new InvalidDataException($"Invalid endpoint: {argumentString}");
                    var host = elements[0];
                    var port = ushort.Parse(elements[1]);
                    return (T)(object)new DnsEndPoint(host, port);
                }

                var argument = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(argumentString);
                return argument;
            }

        }

        class CommandDefinition : CommandDefinitionBase
        {
            Action<CommandContext> _execute;

            public CommandDefinition(string description, Action<CommandContext> execute)
            : base(description)
            {
                _execute = execute;
            }

            public override Action<CommandContext> CreateCommand(Func<string?> argumentProvider)
            {
                return _execute;
            }
        }

        class CommandDefinition<T> : CommandDefinitionBase
        {
            Action<T, CommandContext> _execute;
            public CommandDefinition(string description, Action<T, CommandContext> execute)
            : base(description)
            {
                _execute = execute;
            }

            public override Action<CommandContext> CreateCommand(Func<string?> argumentProvider)
            {
                var argument = ExtractArgument<T>(argumentProvider);
                return ctx => _execute(argument, ctx);
            }
        }
    }


}
