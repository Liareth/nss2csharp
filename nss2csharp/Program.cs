using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace nss2csharp
{
    public class Program
    {
        static void Main(string[] scriptsRawArr)
        {
            List<string> scripts = scriptsRawArr.ToList();

            // Expand wildcards
            for (int i = scripts.Count - 1; i >= 0; --i)
            {
                string script = scripts[i];
                string directory = Path.GetDirectoryName(script);
                string fileName = Path.GetFileName(script);

                if (fileName.Contains("*"))
                {
                    scripts.RemoveAt(i);
                    foreach (string expanded in Directory.GetFiles(directory, fileName))
                    {
                        scripts.Add(expanded);
                    }
                }
            }

            Stopwatch timer = new Stopwatch();

            // Process each file
            foreach (string script in scripts)
            {
                if (File.Exists(script))
                {
                    timer.Restart();

                    Console.WriteLine("Loading {0}", script);
                    string[] sourceFile = File.ReadAllLines(script);

                    if (sourceFile.Length == 0)
                    {
                        Console.WriteLine("Source file empty, skipping.");
                        continue;
                    }

                    Lexer_Nss analysis = new Lexer_Nss();
                    int err = analysis.Analyse(sourceFile.Aggregate((a, b) => a + "\n" + b));
                    if (err != 0)
                    {
                        Console.Error.WriteLine("Failed due to error {1}", err);
                        continue;
                    }

#if DEBUG
                    {
                        int preprocessors = analysis.Tokens.Count(token => token.GetType() == typeof(NssPreprocessor));
                        int comments = analysis.Tokens.Count(token => token.GetType() == typeof(NssComment));
                        int separators = analysis.Tokens.Count(token => token.GetType() == typeof(NssSeparator));
                        int operators = analysis.Tokens.Count(token => token.GetType() == typeof(NssOperator));
                        int literals = analysis.Tokens.Count(token => token.GetType() == typeof(NssLiteral));
                        int keywords = analysis.Tokens.Count(token => token.GetType() == typeof(NssKeyword));
                        int identifiers = analysis.Tokens.Count(token => token.GetType() == typeof(NssIdentifier));

                        Console.WriteLine("DEBUG: Preprocessor: {0} Comments: {1} Separators: {2} " +
                            "Operators: {3} Literals: {4} Keywords: {5} Identifiers: {6}",
                            preprocessors, comments, separators, operators, literals, keywords, identifiers);
                    }

                    {
                        Console.WriteLine("DEBUG: Converting tokens back to source and comparing.");
                        Output_Nss debugOutput = new Output_Nss();

                        string data;
                        err = debugOutput.GetFromTokens(analysis.Tokens, out data);
                        if (err != 0)
                        {
                            Console.Error.WriteLine("DEBUG: Failed due to error {0}", err);
                            continue;
                        }

                        string[] reformattedData = data.Split('\n');

                        int sourceLines = sourceFile.Count();
                        int dataLines = reformattedData.Count();

                        if (sourceLines != dataLines)
                        {
                            Console.Error.WriteLine("DEBUG: Failed due to mismatch in line count. " +
                                "Source: {0}, Data: {1}", sourceLines, dataLines);

                            continue;
                        }

                        for (int i = 0; i < sourceFile.Length; ++i)
                        {
                            string sourceLine = sourceFile[i];
                            string dataLine = reformattedData[i];

                            if (sourceLine != dataLine)
                            {
                                Console.Error.WriteLine("DEBUG: Failed due to mismatch in line contents. " +
                                    "Line {0}.\n" +
                                    "Source line len: {1}\nData line len:   {2}\n" +
                                    "Source line: {3}\nData line:   {4}",
                                    i, sourceLine.Length, dataLine.Length, sourceLine, dataLine);

                                continue;
                            }
                        }

                    }
#endif

                    Console.WriteLine("Processed in {0}ms\n", timer.ElapsedMilliseconds);
                }
                else
                {
                    Console.Error.WriteLine("Failed to read file {0}", script);
                }
            }
        }
    }
}
