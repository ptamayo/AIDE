using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Aide.ApiGateway.Scaffolding
{
    /// <summary>
    /// HEADS UP: Make sure you open Visual Studio with Admin priviledges otherwise it will NOT write the output file.
    /// You can also change the output route if you want.
    /// </summary>
    internal class Program
    {
        static TargetEnvironment Target = TargetEnvironment.Dev;

        static void Main()
        {
            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();

            //configuration.AddJsonFile("appsettings.Primo.json");
            var AIDE_DEV_NAME = Environment.GetEnvironmentVariable("AIDE_DEV_NAME"); // TODO: Fix this, somehow it is not working.
            if (!string.IsNullOrWhiteSpace(AIDE_DEV_NAME))
            {
                Console.WriteLine($"Environmental variable(s) found:");
                Console.WriteLine($"AIDE_DEV_NAME: {AIDE_DEV_NAME}");
                Console.WriteLine($"Loading appsettings.{AIDE_DEV_NAME}.json");
                configuration.AddJsonFile($"appsettings.{AIDE_DEV_NAME}.json", optional: true, reloadOnChange: true);
            }

            var config = configuration.Build();

            var inputFile = config.GetRequiredSection("AppSettings:InputFile");
            var outputFolder = config.GetRequiredSection("AppSettings:OutputFolder");
            var outputFile = Path.Combine(outputFolder.Value, $"ocelot-{Target}-{DateTime.Now.Ticks}.json");

            var ocelotString = File.ReadAllText(inputFile.Value);
            var ocelot = JsonConvert.DeserializeObject<OcelotConfig>(ocelotString);
            foreach (var route in ocelot.Routes)
            {
                route.UpstreamPathTemplate = GetUpstreamPathTemplate(route.UpstreamPathTemplate);

                foreach (var downstreamHostAndPort in route.DownstreamHostAndPorts)
                {
                    switch (Target)
                    {
                        case TargetEnvironment.Dev:
                            // Dev notes: There's no need of any logic here because the entries for DEV are the same as LOCAL.
                            break;
                        case TargetEnvironment.UAT:
                            if (MyDictionary.PortUat.ContainsKey(downstreamHostAndPort.Port))
                            {
                                downstreamHostAndPort.Port = MyDictionary.PortUat[downstreamHostAndPort.Port];
                            }
                            else
                            {
                                Console.WriteLine("Warning: The port number {0} does NOT exist in UAT dictionary.", downstreamHostAndPort.Port);
                            }
                            break;
                        case TargetEnvironment.Prod:
                            // Dev notes: There's no need of any logic here because the entries for PROD are the same as LOCAL.
                            break;
                        case TargetEnvironment.DockerDev:
                            // Change the host reference to host.docker.internal for all endoints.
                            downstreamHostAndPort.Host = "host.docker.internal";
                            break;
                    }
                }
            }

            // Set the base url given the target environment:
            ocelot.GlobalConfiguration.BaseUrl = MyDictionary.GlobalConfigurationBaseUrl[Target];
            ocelotString = JsonConvert.SerializeObject(ocelot);
            System.IO.File.WriteAllText(outputFile, ocelotString);

            Console.WriteLine($"The file has been generated in the followig location: {outputFile}");
            Console.WriteLine("Press any key to finish ...");
            Console.ReadKey();
        }

        static string GetUpstreamPathTemplate(string upstreamPathTemplate)
        {
            // This applies to ALL environments (except for local dev obviouslys)
            return upstreamPathTemplate.Replace("/api", string.Empty);
        }
    }
}
