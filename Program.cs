using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;

namespace UkoaConverter
{
    class Program
    {


        public class Options
        {
            [Option('i', Required = true, HelpText = "Input file path to convert. Output will have same name with .csv extension. Existing files are overwritten.")]
            public string Input { get; set; }
            [Option('s', Default = "\t", Required = false, HelpText = "Separator to use, Tab is default.")]
            public string Separator { get; set; }
            public string Output { get; set; }
        }

        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        private static void RunOptionsAndReturnExitCode(Options options)
        {
            options.Output = Path.Combine(Path.GetDirectoryName(options.Input), new FileInfo(options.Input).Name + ".csv");

            Console.WriteLine($"Input: {options.Input}");
            Console.WriteLine($"Separator: {options.Separator}");
            Console.WriteLine($"Output: {options.Output}");

            using (var fileStream = new FileStream(options.Input, FileMode.Open))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    using (var writer = File.CreateText(options.Output))
                    {
                        while ((line = reader.ReadLine()?.Trim()) != null)
                        {
                            //RAE52-01               0645929.40S 655311.90E
                            var parts = line.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                            //RAE52-01,0645929.40S,655311.90E

                            var profileNumber = parts[0];
                            var shotNumber = parts[1].Substring(0, parts[1].Length - 10);
                            var northSouth = LatLonString2DD(parts[1].Substring(parts[1].Length - 10));
                            var eastWest = LatLonString2DD(parts[2]);

                            //east-west: 655311.90E
                            writer.WriteLine($"{profileNumber}{options.Separator}{shotNumber}{options.Separator}{northSouth:00.000000}{options.Separator}{eastWest:00.000000}");
                        }
                    }
                }
            }
        }

        private static void HandleParseError(IEnumerable<CommandLine.Error> errors)
        {
            foreach (var error in errors)
            {
                Console.WriteLine(error.Tag);
            }
        }

        private static double LatLonString2DD(string latLon)
        {
            var direction = latLon.Last();
            latLon = latLon.Remove(latLon.Length - 1);
            var seconds = double.Parse(latLon.Substring(latLon.Length - 5));
            latLon = latLon.Remove(latLon.Length - 5);
            var minutes = int.Parse(latLon.Substring(latLon.Length - 2));
            latLon = latLon.Remove(latLon.Length - 2);
            var degrees = int.Parse(latLon);
            return DMS2DD(degrees, minutes, seconds, direction);
        }

        private static double DMS2DD(int degrees, int minutes, double seconds, char direction)
        {
            var dd = degrees + minutes / 60 + seconds / 3600;
            return (direction == 'S' || direction == 'W') ? -1 * dd : dd;
        }
    }
}