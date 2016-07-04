using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GedcomConvertDate
{
    class Program
    {
        class Options
        {
            [Option('r', "read", Required = true,
                HelpText = "Input file to be processed.")]
            public string InputFile { get; set; }

            [Option('v', "verbose", DefaultValue = true,
              HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }

        }

        private static int IndexOfNth(string str, char c, int n)
        {
            int s = -1;

            for (int i = 0; i < n; i++)
            {
                s = str.IndexOf(c, s + 1);

                if (s == -1) break;
            }

            return s;
        }


        private static string AbbreviateMonths(string s)
        {
            s = AbbreviateMonths(s, "en-US");
            s = AbbreviateMonths(s, "de-DE");
            return s;

        }

        private static string AbbreviateMonths(string s, string culture)
        {
            CultureInfo ci = new CultureInfo(culture);
            DateTimeFormatInfo dtfi = ci.DateTimeFormat;
            string[] months = dtfi.MonthNames;
            string[] abbrmonths = dtfi.AbbreviatedMonthNames;

            for (int i = 0; i < months.Length; i++)
            {
                if (months[i] != "" && s.Contains(months[i].ToUpper()) && months[i].Length>3)
                {
                    s = s.ToUpper().Replace(months[i].ToUpper(), abbrmonths[i].ToUpper());
                    break;
                }
            }

            return s;
        }

        private static string GetPrefix(string s)
        {
            string[] prefix = s.Split(' ');
            if (prefix.Length <= 0) { return string.Empty; }

            switch (prefix[0].ToUpper().Replace(".",""))
            {
                case "CA":
                case "EST":
                    return "EST";
                case "VOR":
                case "BEF":
                    return "BEF";
                case "NACH":
                case "AFT":
                case "AFTER":
                    return "AFT";
                case "UM":
                case "ABT":
                case "ABOUT":
                    return "ABT";                    
                default:
                    return string.Empty;
            }
        }

        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                if (options.Verbose) Console.WriteLine("Filename: {0}", options.InputFile);
            }            

            if (!System.IO.File.Exists(options.InputFile))
            {
                Console.WriteLine("No input file given or file not found!");
                return;
            }

            StreamReader reader = File.OpenText(options.InputFile);

            FileInfo fi = new FileInfo(options.InputFile);
            string outputFile = fi.DirectoryName + "\\" + fi.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + fi.Extension;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputFile, false))
            {
                string line;
                int lineCount = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineCount++;
                    string[] items;
                    int level = 0;

                    try
                    {
                        items = line.Split(' ');
                        level = int.Parse(items[0]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Cannot parse file. This application can process only GEDCOM files.");
                        if (System.IO.File.Exists(outputFile))
                        {
                            file.Close();
                            File.Delete(outputFile);
                        }
                        return;
                    }

                    string cmd = items[1].ToString();
                    string data = String.Empty;
                    if (items.Length >= 3)
                    {
                        data = line.Substring(IndexOfNth(line, ' ', 2)).Trim().Replace(",","");
                    }

                    if (cmd == "DATE" && (!data.Equals(String.Empty)))
                    {

                        String dateOutput = String.Empty;
                        DateTime dateConverted = new DateTime();

                        bool failed = false;
                        String datePrefix = GetPrefix(data);

                        data = datePrefix.Length==0?data: data.Substring(IndexOfNth(data, ' ', 1)).Trim().Replace(",", "");                        

                        if (data == String.Empty)
                        {
                            dateOutput = String.Empty;
                        }
                        else if (data.Trim().Length == 4)
                        {
                            dateOutput = data.Trim();
                        }
                        else
                        {
                            int count = data.Split('.').Length - 1;
                            if (count == 1)
                            {
                                if (data.Split(' ').Length - 1 == 1)
                                {
                                    CultureInfo culture = CultureInfo.InvariantCulture;
                                    DateTimeStyles styles = DateTimeStyles.None;
                                    if (DateTime.TryParse(data, culture, styles, out dateConverted))
                                    {
                                        dateOutput = dateConverted.ToString("MMM yyyy", CultureInfo.InvariantCulture).ToUpper();
                                    }
                                    else
                                    {
                                        dateOutput = data;
                                        failed = true;
                                    }
                                }
                                else if (data.Split(' ').Length - 1 == 2)
                                {
                                    CultureInfo culture = new CultureInfo("de-DE");
                                    DateTimeStyles styles = DateTimeStyles.None;
                                    if (DateTime.TryParse(data, culture, styles, out dateConverted))
                                    {
                                        dateOutput = dateConverted.ToString("dd MMM yyyy", CultureInfo.InvariantCulture).ToUpper();
                                    }
                                    else
                                    {
                                        dateOutput = data;
                                        failed = true;
                                    }
                                }
                                else
                                {
                                    CultureInfo culture = CultureInfo.InvariantCulture;
                                    DateTimeStyles styles = DateTimeStyles.None;
                                    if (DateTime.TryParse(data, culture, styles, out dateConverted))
                                    {
                                        dateOutput = dateConverted.ToString("MMM yyyy", CultureInfo.InvariantCulture).ToUpper();
                                    }
                                    else
                                    {
                                        dateOutput = data;
                                        failed = true;
                                    }
                                }
                            }
                            else if (count == 2)
                            {
                                CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
                                DateTimeStyles styles = DateTimeStyles.None;
                                if (DateTime.TryParse(data, culture, styles, out dateConverted))
                                    dateOutput = dateConverted.ToString("dd MMM yyyy", CultureInfo.InvariantCulture).ToUpper();
                                else
                                {
                                    dateOutput = data;
                                    failed = true;
                                }
                            }
                            else
                            {
                                dateOutput = data;
                                // Check for format mm/dd/yyyy                                
                                if (data.Split('/').Length - 1 == 2)
                                {
                                    CultureInfo culture = new CultureInfo("en-US");
                                    DateTimeStyles styles = DateTimeStyles.None;
                                    if (DateTime.TryParse(data, culture, styles, out dateConverted))
                                    {
                                        dateOutput = dateConverted.ToString("dd MMM yyyy", CultureInfo.InvariantCulture).ToUpper();
                                    }
                                    else
                                    {
                                        dateOutput = data;
                                        failed = true;
                                    }
                                } else
                                // Check for format ddd mm yyyy
                                if(data.Split(' ').Length - 1 == 2)
                                {
                                    CultureInfo culture = new CultureInfo("en-US");
                                    DateTimeStyles styles = DateTimeStyles.None;
                                    if (DateTime.TryParse(data, culture, styles, out dateConverted))
                                    {
                                        dateOutput = dateConverted.ToString("dd MMM yyyy", CultureInfo.InvariantCulture).ToUpper();
                                    }
                                    else
                                    {
                                        dateOutput = data;
                                        failed = true;
                                    }
                                }
                            }

                        }
                        dateOutput = datePrefix + (datePrefix.Length ==0? "":" " ) + AbbreviateMonths(dateOutput.ToUpper()).Replace("  ", " ");
                        if (options.Verbose)
                        {
                            Console.WriteLine(string.Format("Line {0}:{1} {2} {3} {4}", lineCount, cmd, data, dateOutput, failed));
                        }
                        file.WriteLine(string.Format("{0} {1} {2}", level, cmd, dateOutput));
                        //file.WriteLine(String.Format("{0}\t{1}\t\t\t{2}\t\t\t{3}", cmd, data, dateOutput, failed));
                    }
                    else
                    {
                        if (cmd !="_APID")
                            file.WriteLine(line);
                        
                    }
                    file.Flush();
                }
            }
        }
    }
}
