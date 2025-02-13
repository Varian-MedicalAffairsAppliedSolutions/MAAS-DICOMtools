using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using DicomTools.Retrieve;
using DicomTools.SearchTag;
using DicomTools.Show;
using DicomTools.Store;
using FellowOakDicom;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DicomTools
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand(description: "DicomTools contains 4 different Dicom tools.")
            {
                new RetrieveCommand(),
                new StoreCommand(),
                new SearchTagCommand(),
                new ShowCommand()
            };

            // Use Host.CreateDefaultBuilder as CommandLineBuilder.UseHost does not initialize
            // configuration and logging properly.
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configure =>
                {
                    configure.SetBasePath(Path.GetDirectoryName(typeof(Program).Assembly.Location)!);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<DicomAnonymizer>();
                    services.AddSingleton<DicomStorageRetrieveOptions>();
                    services.AddHostedService<DicomStoreService>();
                    services.AddFellowOakDicom();
                    services.AddTransient<IConfirmationService, ConfirmationService>();
                });

            var commandLineParser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseHost(_ => hostBuilder)
                .AddMiddleware(async (context, next) =>
                {
                    // Hook up host services to CommandLine.
                    var host = (IHost) context.BindingContext.GetService(typeof(IHost))!;
                    context.BindingContext.AddService(_ => host.Services);
                    await next(context);
                })
                .Build();

            return await commandLineParser.InvokeAsync(args);
        }
    }
}
