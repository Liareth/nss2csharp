using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nss2csharp
{
    public enum NssType
    {
        Int,
        Float,
        String,
        Struct,
        Object,
        Location,
        Vector,
        ItemProperty,
        Effect
    }

    public class NssNode
    {
    }

    public class NssCompilationUnit : NssNode
    {
        public string m_Name; // Script name
        public List<NssNode> m_Children = new List<NssNode>(); // Constants, declarations, implementations - in order of original source file.
    }

    public class NssFunctionDeclaration : NssNode
    {
        public string m_Name;
        public List<NssLvalue> m_Parameters;
    }

    public class NssFunctionImplementation : NssNode
    {
        public string m_Name;
        public List<NssLvalue> m_Parameters;
        public List<NssNode> m_Children = new List<NssNode>(); // The function body - in order of original source file.
    }

    public class NssConstant : NssNode
    {
        public NssType m_Type; // A constant can only be an int, string, or float.
        public string m_Name; // The actual name of this constant.
        public string m_Value; // It's safe for us to store the value as a string as we never really need to process it.
    }

    public class NssLvalue : NssNode
    {
        public NssType m_Type;
        public string m_Name;
    }

    public class NssFunctinParameter : NssLvalue
    {
        public string m_Defaultvalue; // Contains the default value in string format - if there is one. Else, null.
    }

    public class NssStruct : NssNode
    {
    }

    public class NssParser
    {
        public int Parse(string name, List<NssLexToken> tokens, out NssCompilationUnit cu)
        {
            cu = new NssCompilationUnit();
            cu.m_Name = name;

            return 1;
        }
    }

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
                    NssLexicalAnalysis analysis = new NssLexicalAnalysis();
                    List<NssLexToken> lexTokens;
                    int err = analysis.Analyse(File.ReadAllLines(script), out lexTokens);
                    if (err != 0)
                    {
                        Console.Error.WriteLine("Failed to analyse {0} due to error {1}", script, err);
                    }

                    NssParser parser = new NssParser();
                    NssCompilationUnit cu;
                    err = parser.Parse(Path.GetFileName(script), lexTokens, out cu);
                    if (err != 0)
                    {
                        Console.Error.WriteLine("Failed to parse {0} due to error {1}", script, err);
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
