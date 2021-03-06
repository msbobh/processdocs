﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.IO;
// using System.Runtime.InteropServices;
using Stemmer;
using SautinSoft.Document;

namespace processdocs
{
    // iText is a PDF library that allows you to CREATE, ADAPT, INSPECT and MAINTAIN
    // documents in the Portable Document Format (PDF), allowing you to add PDF 
    // functionality to your software projects with ease.  We even have documentation 
    // to help you get coding.

    // Sautinsoft Document .Net is 100% C# managed library which gives you API to create, parse, 
    // load, modify, convert, edit and merge documents in PDF, DOCX, RTF, HTML and 
    // Text formats. Rasterize documents to Images and render to WPF FrameworkElement.

    class Program
    {
        // *******************
        // Sub routines
        // *******************
        private static bool fileopen = true;
        private static int errorcount = 0;
        private static bool debug = false;

        static private string GetTextFromPDF(string fname)
        {
            StringBuilder text = new StringBuilder();
            try {
                using (PdfReader reader = new PdfReader(fname))
                {
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on pdf open {0}", e);
                fileopen = false;
            }

            return text.ToString();
        }
        
                
        // Reading Text from Word docx, pdf or rtf file
        static private string GetTextFromDocs(string fname)
        {
            fileopen = true;    // Reset fileopen error flag
            if (fname.Contains(".pdf"))
            {
                return GetTextFromPDF(fname);
                
            }
            else
            {
                try
                {
                    DocumentCore dc = DocumentCore.Load(fname);
                    StringBuilder text = new StringBuilder();
                    foreach (Run run in dc.GetChildElements(true, ElementType.Run))
                    {
                        // Console.WriteLine(run.Text);
                        text.Append(" \r\n " + run.Text.ToString());
                    }
                    return text.ToString();
                }
                catch (Exception e)
                {
                    if (debug == true)
                    {
                        Console.WriteLine("File Error ==>{0}", e);
                        Console.WriteLine("Error on file {0}", fname);
                    }
                    else
                    {
                        Console.WriteLine("Error on file {0}", fname);
                        errorcount = errorcount + 1;                    }

                    fileopen = false;
                    return null;
                }
            }
        }

        //
        // strip out URLs
        //
        static private string RemoveURL(string currentdoc)
        {
            return System.Text.RegularExpressions.Regex.Replace(currentdoc, @"https?://\S+", "httpaddr");

        }

        //
        // Strip out numbers
        //
        static private string removeNumbers(string currentdoc)
        {
            // First action strips out the single digits and replaces with an empty string
            // final line removes the remaining number formats including exponents
            currentdoc = System.Text.RegularExpressions.Regex.Replace(currentdoc, @"[\d]", string.Empty);
            return System.Text.RegularExpressions.Regex.Replace(currentdoc, @"\$?[0-9]+[\.[0-9]+", "number");
                        
        }

        //
        // Strip out punctuation
        //
        static private string Strippunctuation(string currentdoc)
        {


            // \W	Matches any non-word character. Equivalent to "[^A-Za-z0-9_]".
            currentdoc = System.Text.RegularExpressions.Regex.Replace(currentdoc, @"\W", " ");
            // Strip out extra spaces.
            return System.Text.RegularExpressions.Regex.Replace(currentdoc, " +", " ");
        }
        //
        // Strip out eamil addresses
        //
        static private string StripEmails(string currentdoc)
        {
            return System.Text.RegularExpressions.Regex.Replace(currentdoc, @"\S+\@\S+", "emailwashere");
        }
        //
        // Lowercase all the words
        //
        static private string lowercase(string currentdoc)
        {

            return currentdoc.ToLower();

        }

        static private string processdocument(string inputfile)
        {
            string currentdoc = GetTextFromDocs(inputfile);
            if (fileopen == true)
            {
                currentdoc = lowercase(currentdoc);
                currentdoc = RemoveURL(currentdoc);
                currentdoc = removeNumbers(currentdoc);
                currentdoc = StripEmails(currentdoc);
                currentdoc = Strippunctuation(currentdoc);
            }
            return currentdoc;
        }

        static void Main(string[] args)
        {
            debug = false; // If set increases output of info
            string usage = "Usage options: \"verbose\" for additional output ";
            
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "verbose":
                        debug = true;
                        break;

                    default:
                        Console.WriteLine(usage);
                        break;
                }
            }
                  
            
            string output;
            string[] tokens;
            StreamWriter outfile = new StreamWriter("dictionary.txt");

            Console.WriteLine("Getting list of files for processing");
            //*************************************
            // Get files from the current directory
            //*************************************
            string path = Directory.GetCurrentDirectory();
            string[] docx = Directory.GetFiles(path,"*.docx");
            string[] pdf = Directory.GetFiles(path,"*.pdf");
            string[] rtf = Directory.GetFiles(path, "*.rtf");
            string[] docArray = docx.Concat(Directory.GetFiles(path, "*.pdf")).ToArray();
            docArray = docArray.Concat(rtf).ToArray();
            Array.Sort(docArray);   // Ensure sort order is maintained across the processing apps

            // list all docs found
            if (debug == true)
            {
                Console.WriteLine("We found the following list of files: ");
                foreach (var file in docArray)
                {
                    Console.WriteLine(file);
                }
            }
            Console.WriteLine("Total Files found:{0}", docArray.Length);

            Dictionary<string, int> dictionary = new Dictionary<string, int>();

            foreach (var file in docArray)
            {
                output = processdocument(file);
                if (fileopen == false)
                    continue;
                string fname = file;
                
                fname = file.Replace(".docx", ".txt").Replace(".pdf", ".txt").Replace(".rtf", ".txt");
                
                Console.WriteLine($"Writing file {fname} output...");
                System.IO.StreamWriter writefile = new System.IO.StreamWriter(fname, true); // Create output file same name .txt

                //*************
                // tokenization
                //*************
                char[] separators = { '_',' ', ',', '.', '-', ':', ';', '{', '}', '|', '\n', '\t', '\u2029', '\r' };
                tokens = output.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                var stemmer = new PorterStemmer();

                string stem; // Token after stemming
                foreach (var token in tokens)
                {
                    // Stem the current Token stemmed token left in stem

                    stem = stemmer.StemWord(token);
                    writefile.WriteLine(stem);

                    // The Add method throws an exception if the new key is 
                    // already in the dictionary.
                    try
                    {
                        dictionary.Add(stem, 1);
                    }
                    catch (ArgumentException)
                    {
                        dictionary[stem] += 1;  // if exists increment the count
                    }


                }
                writefile.Close(); // added this becuase for resumes of less than a page the file was empty
            }

            // Now write out the dicionary to a text file
            if (fileopen == true )
            {
                foreach (var entry in dictionary)
                {
                    outfile.WriteLine("{0}, {1}", entry.Key, entry.Value);
                }
                outfile.Close(); // added this because for resumes of less than a page the dict was empty
            }
            Console.WriteLine("{0} Errors found", errorcount);
        }
    }
}
    

