using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using System.Globalization;

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

        private static void ProcessSingleFile(string input, string output, string separator)
        {
            Console.WriteLine($"Input: {input}");
            Console.WriteLine($"Separator: {separator}");
            Console.WriteLine($"Output: {output}");

            using (var fileStream = new FileStream(input, FileMode.Open))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    using (var writer = File.CreateText(output))
                    {
                        NumberFormatInfo nfi = new NumberFormatInfo();
                        nfi.NumberDecimalSeparator = ".";
                        nfi.NumberDecimalDigits = 6;
                        while ((line = reader.ReadLine()?.Trim()) != null)
                        {
                            //RAE52-01               0645929.40S 655311.90E
                            var profileAndShot = line.Substring(0, line.Length - 21).Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                            var profileNumber = profileAndShot[0];
                            var shotNumber = profileAndShot[1];
                            var northSouth = LatLonString2DD(line.Substring(line.Length - 21, 10));
                            var eastWest = LatLonString2DD(line.Substring(line.Length-11));

                            //east-west: 655311.90E
                            writer.WriteLine($"{profileNumber}{separator}{shotNumber}{separator}{northSouth.ToString("N", nfi)}{separator}{eastWest.ToString("N", nfi)}");
                        }
                    }
                }
            }
        }

        private static void RunOptionsAndReturnExitCode(Options options)
        {
            // check if input is directory or file
            if (Directory.Exists(options.Input))
            {
                // get all .uko files and process them iteratively
                foreach (var ukoFile in Directory.EnumerateFiles(options.Input, "*.uko"))
                {
                    ProcessSingleFile(ukoFile, GetOutputPath(ukoFile), options.Separator);
                }
            }
            else if (File.Exists(options.Input))
            {
                ProcessSingleFile(options.Input, GetOutputPath(options.Input), options.Separator);
            }
            else
            {
                Console.WriteLine($"Input folder or file ({options.Input}) does not exist.");
            }
        }

        private static string GetOutputPath(string inputPath)
        {
            return Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath) + ".csv");
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