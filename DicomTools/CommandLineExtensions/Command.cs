using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;

namespace DicomTools.CommandLineExtensions
{
    public abstract class Command<TOptions, TOptionsHandler> : Command
        where TOptions : class, ICommandOptions
        where TOptionsHandler : class, ICommandOptionsHandler<TOptions>
    {
        public Command(string name, string description) : base(name, description)
        {
            Handler = CommandHandler.Create<TOptions, IServiceProvider, CancellationToken>(HandleOptions);
        }

        protected Option<T> AddOption<T>(string name, string description, bool isRequired, T? defaultValue)
        {
            var option = new Option<T>(name, description)
            {
                IsRequired = isRequired
            };
            if (defaultValue != null)
            {
                if (defaultValue is string stringValue)
                {
                    if (!string.IsNullOrEmpty(stringValue))
                        option.SetDefaultValue(stringValue);
                }
                else
                {
                    option.SetDefaultValue(defaultValue);
                }
            }

            AddOption(option);
            return option;
        }

        private static async Task<int> HandleOptions(TOptions options, IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var handler = ActivatorUtilities.CreateInstance<TOptionsHandler>(serviceProvider);
            return await handler.HandleAsync(options, cancellationToken);
        }
    }
}
