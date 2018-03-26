using System;
using System.Collections.Generic;
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

            // Process each file
            foreach (string script in scripts)
            {
                if (File.Exists(script))
                {
                    Lexer_Nss analysis = new Lexer_Nss();

                    Console.WriteLine("Analysing {0}", script);
                    int err = analysis.Analyse(File.ReadAllLines(script).Select(str => str + "\n"));
                    if (err != 0)
                    {
                        Console.Error.WriteLine("Failed to analyse {0} due to error {1}", script, err);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Failed to read file {0}", script);
                }
            }
        }
    }
}
