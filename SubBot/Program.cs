﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace DevSubmarine.SubBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            EnableUnhandledExceptionLogging();

            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsecrets.json", optional: true);
                    config.AddJsonFile($"appsecrets.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // configure options

                    // add framework services

                    // add Discord client

                    // add handlers

                })
                .UseSerilog((context, config) => ConfigureSerilog(context, config), true)
                .Build();
            await host.RunAsync().ConfigureAwait(false);
        }

        private static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration config)
        {
            config.ReadFrom.Configuration(context.Configuration);
            DatadogOptions ddOptions = context.Configuration.GetSection("Serilog")?.GetSection("DataDog")?.Get<DatadogOptions>();
            if (ddOptions != null)
            {
                config.WriteTo.DatadogLogs(
                    ddOptions.ApiKey,
                    source: ".NET",
                    service: ddOptions.ServiceName ?? "SubBot-CS",
                    host: ddOptions.HostName ?? Environment.MachineName,
                    new string[] {
                                $"env:{(ddOptions.EnvironmentName ?? context.HostingEnvironment.EnvironmentName)}",
                                $"assembly:{(ddOptions.AssemblyName ?? context.HostingEnvironment.ApplicationName)}"
                    },
                    ddOptions.ToDatadogConfiguration(),
                    // no need for debug logs in datadag
                    logLevel: ddOptions.OverrideLogLevel ?? LogEventLevel.Verbose
                );
            }
        }

        private static void EnableUnhandledExceptionLogging()
        {
            // add default logger for errors that happen before host runs
            Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File("logs/unhandled.txt",
                        fileSizeLimitBytes: 1048576,        // 1MB
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 5,
                        rollingInterval: RollingInterval.Day)
                        .CreateLogger();
            // capture unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Logger.Error((Exception)e.ExceptionObject, "An exception was unhandled");
                Log.CloseAndFlush();
            }
            catch { }
        }
    }
}
