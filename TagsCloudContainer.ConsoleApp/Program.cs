﻿using Autofac;
using CommandLine;
using CommandLine.Text;
using TagsCloudContainer.Infrastructure;
using TagsCloudContainer.Infrastructure.Settings;

namespace TagsCloudContainer.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ExceptionHandler.HandleExceptionsFrom(() =>
            {
                if(!TryParseOptions(args, out var options, out var message))
                {
                    Console.WriteLine(message);
                    return;
                }

                var container = ConfigureAndBuildContainer(options!);
                container.Resolve<Application>().Run();
            }, e => Console.Write(e.Message));
        }

        private static bool TryParseOptions(string[] args, out Options? options, out string message) 
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            options = parserResult.Value;
            message = string.Empty;

            if (parserResult.Errors.Any())
            {
                var parserErrors = parserResult.Errors.Where(e => e.Tag == ErrorType.BadVerbSelectedError).ToArray();
                if (!parserErrors.Any())
                    return false;

                var helpText = HelpText.AutoBuild(parserResult,
                                                  ht => HelpText.DefaultParsingErrorsHandler(parserResult, ht));
                message = helpText.ToString();
                return false;
            }

            return true;
        }

        private static IContainer ConfigureAndBuildContainer(Options options)
        {
            var builder = new ContainerBuilder();

            builder.Register(ctx => new ConsoleSettingsProvider(options)).As<ISettingsProvider>().SingleInstance();
            builder.Register(ctx => ctx.Resolve<ISettingsProvider>().GetWordColorSettings()).AsSelf();
            builder.Register(ctx => ctx.Resolve<ISettingsProvider>().GetWordFontSettings()).AsSelf();
            builder.Register(ctx => ctx.Resolve<ISettingsProvider>().GetOutputImageSettings()).AsSelf();
            builder.Register(ctx => ctx.Resolve<ISettingsProvider>().GetTextReaderSettings()).AsSelf();

            builder.RegisterType<TextFileReader>().As<IWordReader>().SingleInstance();
            builder.RegisterType<WordPreparer>().As<IWordPreparer>().SingleInstance();

            builder.RegisterType<WordLinearColorProviderFactory>().As<IWordColorProviderFactory>().SingleInstance();
            builder.RegisterType<WordLinearFontSizeProviderFactory>().As<IWordFontSizeProviderFactory>().SingleInstance();

            builder.RegisterType<CircularWordLayoutBuilder>().As<IWordLayoutBuilder>();
            builder.RegisterType<TagsCloudGenerator>().As<ITagsCloudGenerator>();

            builder.RegisterType<WordPlateVisualizer>().AsSelf().SingleInstance();

            builder.RegisterType<Application>().AsSelf().SingleInstance();

            return builder.Build();
        }
    }
}