using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

        class Objects
        {
            public String file;

            /* 
             * MULTIMEDIA_FORMAT:
             * [bmp | gif | jpg | ole | pcx | tif | wav] 
             */
            public String form;

            /*
             * SOURCE_MEDIA_TYPE:={Size=1:15}
             * [ audio | book | card | electronic | fiche | film | magazine | manuscript | map | newspaper | photo | tombstone | video]
             */
            public String type;

            public int id;

            public Objects(int id)
            {
                this.id = id;
            }
            
            public String GetObjectIDLine(int level)
            {
                //1 OBJE @M14@
                return String.Format("{0} OBJE @O{1}@", level, this.id);
            }

            public List<String> GetObjectOutputLines(int level)
            {
                List<String> output = new List<string>();
                Regex r = new Regex("[^A-Z0-9 ]+",RegexOptions.IgnoreCase);
                String result = r.Replace(file, "");
                if (result.Length > 49)
                    result = result.Substring(0, 49);
                output.Add(String.Format("{0} @O{1}@ OBJE", level, this.id));
                output.Add(String.Format("{0} FILE {1}", level+1, result+"."+this.form));
                output.Add(String.Format("{0} FORM {1}", level+2, this.form));
                output.Add(String.Format("{0} TYPE {1}", level+3, this.type));
                return output;
            }
        }

        public static int IndexOfNth(string str, char c, int n)
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

            switch (prefix[0].ToUpper().Replace(".", ""))
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
            int previousLevel = 0;
            Objects multimediaObjects= new Objects(0);
            int objectsID = 0;

            FileInfo fi = new FileInfo(options.InputFile);
            string outputFile = fi.DirectoryName + "\\" + fi.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + fi.Extension;


            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputFile, false))
            {
                string line;
                int lineCount = 0;
                bool parseObjects = false;
                int objectsLevel = 0;
                String tempFile = String.Empty;
                String tempForm = String.Empty;
                List<Objects> objectsList = new List<Objects>();

                while ((line = reader.ReadLine()) != null)
                {
                    lineCount++;
                    string[] items;
                    int level = 0;

                    try
                    {
                        items = line.Split(' ');
                        previousLevel = level;
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
                                                                
                    bool ignoreLine = false;

                    if (parseObjects == true)
                    {
                        if (level == objectsLevel)
                        {
                            if (cmd=="FORM")
                            {
                                tempForm = data;
                                ignoreLine = true;
                            } else if (cmd == "TITL")
                            {
                                tempFile = data;
                                ignoreLine = true;
                            } else
                            {
                                //Ignore
                                ignoreLine = true;
                            }
                        } else if (level == objectsLevel -1)
                        {
                            multimediaObjects.file = tempFile;
                            multimediaObjects.form = tempForm;
                            multimediaObjects.type = "photo";
                            objectsList.Add(multimediaObjects);                            
                            file.WriteLine(multimediaObjects.GetObjectIDLine(objectsLevel));

                            if (cmd == "OBJE")
                            {
                                objectsLevel = level + 1;                                
                                multimediaObjects = new Objects(++objectsID);
                                ignoreLine = true;
                                parseObjects = true;
                            }
                            else
                            {
                                ignoreLine = false;
                                parseObjects = false;
                            }
                        } else
                        {
                            
                        }
                    } else
                    {
                        if (cmd == "_APID")
                            ignoreLine = true;
                        else if (cmd=="PAGE" && level == 2)                        
                            ignoreLine = true;                        
                        else if (cmd == "DATA" && level == 2)
                            ignoreLine = true;
                        else if (cmd == "TEXT" && level == 3)
                            ignoreLine = true;                        
                        else if (cmd == "DATE" && (!data.Equals(String.Empty)))
                        {

                            String dateOutput = String.Empty;
                            DateTime dateConverted = new DateTime();

                            bool failed = false;
                            String datePrefix = GetPrefix(data);

                            data = datePrefix.Length == 0 ? data : data.Substring(IndexOfNth(data, ' ', 1)).Trim().Replace(",", "");

                            dateOutput = ConvertDate(data, ref dateConverted, ref failed, datePrefix);
                            if (options.Verbose)
                            {
                                Console.WriteLine(string.Format("Line {0}:{1} {2} {3} {4}", lineCount, cmd, data, dateOutput, failed));
                            }
                            file.WriteLine(string.Format("{0} {1} {2}", level, cmd, dateOutput));
                            //file.WriteLine(String.Format("{0}\t{1}\t\t\t{2}\t\t\t{3}", cmd, data, dateOutput, failed));
                            ignoreLine = true;
                        }
                        else if (cmd == "OBJE")
                        {
                            parseObjects = true;
                            objectsLevel = level + 1;
                            multimediaObjects = new Objects(++objectsID);
                            ignoreLine = true;
                        } else if (cmd=="TRLR")
                        {
                            // Add objects
                            foreach (Objects objects in objectsList)
                            {
                                foreach (String list in objects.GetObjectOutputLines(0))
                                {
                                    file.WriteLine(list);
                                }
                            }

                            ignoreLine = false;
                        }

                    }

                    if (!ignoreLine)
                        file.WriteLine(line);
                        
                    
                    file.Flush();
                }
                
                Console.WriteLine("Finished.");
            }

            Console.WriteLine("Finished.");
        }

        private static string ConvertDate(string data, ref DateTime dateConverted, ref bool failed, string datePrefix)
        {
            string dateOutput;
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
                    }
                    else
                    // Check for format ddd mm yyyy
                    if (data.Split(' ').Length - 1 == 2)
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
            dateOutput = datePrefix + (datePrefix.Length == 0 ? "" : " ") + AbbreviateMonths(dateOutput.ToUpper()).Replace("  ", " ");
            return dateOutput;
        }
    }
}
