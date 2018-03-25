using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nss2csharp
{
    public class NssLexToken
    {

    }

    public enum NssLexKeywords
    {
        If,
        Else,
        For,
        While,
        Do,
        Switch,
        Break,
        Return,
        Case,
        Const,
        Void,
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

    public class NssLexKeyword : NssLexToken
    {
        public override string ToString() { return "[Keyword] " + m_Keyword.ToString(); }

        public static Dictionary<string, NssLexKeywords> Map = new Dictionary<string, NssLexKeywords>
        {
            { "if",           NssLexKeywords.If },
            { "else",         NssLexKeywords.Else },
            { "for",          NssLexKeywords.For },
            { "while",        NssLexKeywords.While },
            { "switch",       NssLexKeywords.Switch },
            { "break",        NssLexKeywords.Break },
            { "return",       NssLexKeywords.Return },
            { "case",         NssLexKeywords.Case },
            { "const",        NssLexKeywords.Const },
            { "void",         NssLexKeywords.Void },
            { "int",          NssLexKeywords.Int },
            { "float",        NssLexKeywords.Float },
            { "string",       NssLexKeywords.String },
            { "object",       NssLexKeywords.Object },
            { "location",     NssLexKeywords.Location },
            { "vector",       NssLexKeywords.Vector },
            { "itemproperty", NssLexKeywords.ItemProperty },
            { "effect",       NssLexKeywords.Effect },
        };

        public NssLexKeywords m_Keyword;
    }

    public class NssLexIdentifier : NssLexToken
    {
        public override string ToString() { return "[Identifier] " + m_Identifier; }

        public string m_Identifier;
    }

    public enum NssLexSeparators
    {
        Whitespace,
        NewLine,
        OpenParen,
        CloseParen,
        OpenCurlyBrace,
        CloseCurlyBrace,
        Semicolon,
        Tab,
        Comma
    }

    public class NssLexSeparator : NssLexToken
    {
        public override string ToString() { return "[Separator] " + m_Separator.ToString(); }

        public static Dictionary<char, NssLexSeparators> Map = new Dictionary<char, NssLexSeparators>
        {
            { ' ',  NssLexSeparators.Whitespace },
            { '\n', NssLexSeparators.NewLine },
            { '(',  NssLexSeparators.OpenParen },
            { ')',  NssLexSeparators.CloseParen },
            { '{',  NssLexSeparators.OpenCurlyBrace },
            { '}',  NssLexSeparators.CloseCurlyBrace },
            { ';',  NssLexSeparators.Semicolon },
            { '\t', NssLexSeparators.Tab },
            { ',',  NssLexSeparators.Comma }
        };

        public NssLexSeparators m_Separator;
    }

    public enum NssLexOperators
    {
        Addition,
        Division,
        Multiplication,
        Modulo,
        And,
        Or,
        Not,
        Inversion,
        GreaterThan,
        LessThan,
        Equals,
        TernaryQuestionMark,
        TernaryColon,
    }

    public class NssLexOperator : NssLexToken
    {
        public override string ToString() { return "[Operator] " + m_Operator.ToString(); }

        public static Dictionary<char, NssLexOperators> Map = new Dictionary<char, NssLexOperators>
        {
            { '+',  NssLexOperators.Addition },
            { '/',  NssLexOperators.Division },
            { '*',  NssLexOperators.Multiplication },
            { '%',  NssLexOperators.Modulo },
            { '&',  NssLexOperators.And },
            { '|',  NssLexOperators.Or },
            { '!',  NssLexOperators.Not },
            { '~',  NssLexOperators.Inversion },
            { '>',  NssLexOperators.GreaterThan },
            { '<',  NssLexOperators.LessThan },
            { '=',  NssLexOperators.Equals },
            { '?',  NssLexOperators.TernaryQuestionMark },
            { ':',  NssLexOperators.TernaryColon },
        };

        public NssLexOperators m_Operator;
    }

    public enum NssLexLiteralType
    {
        Int,
        Float,
        String,
    }

    public class NssLexLiteral : NssLexToken
    {
        public override string ToString() { return "[Literal] " + m_LiteralType.ToString() + ": " + m_Literal; }

        public NssLexLiteralType m_LiteralType;
        public string m_Literal;
    }

    public enum NssLexCommentType
    {
        LineComment,
        BlockComment
    }

    public class NssLexComment : NssLexToken
    {
        public override string ToString() { return "[Comment] " + m_CommentType.ToString() + ": " + m_Comment; }

        public NssLexCommentType m_CommentType;
        public string m_Comment;
    }

    public enum NssLexPreprocessorType
    {
        Unknown
    }

    public class NssLexPreprocessor : NssLexToken
    {
        public override string ToString() { return "[Preprocessor] " + m_PreprocessorType.ToString() + ": " + m_Data; }

        public NssLexPreprocessorType m_PreprocessorType;
        public string m_Data;
    }

    public class NssLexicalAnalysis
    {
        public int Analyse(string[] data, out List<NssLexToken> tokens)
        {
            tokens = new List<NssLexToken>();

            // We're given lines without line endings - so let's add them.
            string aggregatedData = data.Aggregate((a, b) => a + "\n" + b);

            int chBaseIndex = 0;
            while (chBaseIndex < aggregatedData.Length)
            {
                char ch = aggregatedData[chBaseIndex];

                { // PREPROCESSOR
                    if (ch == '#')
                    {
                        // Just scan for a new line or eof, then add this in.
                        int chScanningIndex = chBaseIndex;
                        while (++chScanningIndex < aggregatedData.Length)
                        {
                            char chScanning = aggregatedData[chScanningIndex];

                            bool eof = chScanningIndex == aggregatedData.Length - 1;
                            bool newLine = NssLexSeparator.Map.ContainsKey(chScanning) && NssLexSeparator.Map[chScanning] == NssLexSeparators.NewLine;

                            if (eof || newLine)
                            {
                                NssLexPreprocessor preprocessor = new NssLexPreprocessor();
                                preprocessor.m_PreprocessorType = NssLexPreprocessorType.Unknown;
                                int length = eof ? chScanningIndex - chBaseIndex : chScanningIndex - chBaseIndex - 1;
                                preprocessor.m_Data = aggregatedData.Substring(chBaseIndex + 1, length);
                                tokens.Add(preprocessor);
                                chBaseIndex = eof ? chScanningIndex + 1 : chScanningIndex;
                                break;
                            }
                        }

                        continue;
                    }
                }

                { // COMMENTS
                    if (ch == '/')
                    {
                        int chNextIndex = chBaseIndex + 1;
                        if (chNextIndex < aggregatedData.Length)
                        {
                            bool foundComment = false;

                            char nextCh = aggregatedData[chNextIndex];
                            if (nextCh == '/')
                            {
                                // Line comment - scan for end of line, and collect.
                                int chScanningIndex = chNextIndex;
                                while (++chScanningIndex < aggregatedData.Length)
                                {
                                    char chScanning = aggregatedData[chScanningIndex];

                                    bool eof = chScanningIndex == aggregatedData.Length - 1;
                                    bool newLine = NssLexSeparator.Map.ContainsKey(chScanning) && NssLexSeparator.Map[chScanning] == NssLexSeparators.NewLine;

                                    if (eof || newLine)
                                    {
                                        NssLexComment comment = new NssLexComment();
                                        comment.m_CommentType = NssLexCommentType.LineComment;
                                        int length = eof ? chScanningIndex - chNextIndex : chScanningIndex - chNextIndex - 1;
                                        comment.m_Comment = aggregatedData.Substring(chNextIndex + 1, length);
                                        tokens.Add(comment);
                                        chBaseIndex = eof ? chScanningIndex + 1 : chScanningIndex;
                                        break;
                                    }
                                }

                                foundComment = true;
                            }
                            else if (nextCh == '*')
                            {
                                // Block comment - scan for */, ignoring everything else.
                                int chScanningIndex = chNextIndex;
                                while (++chScanningIndex < aggregatedData.Length)
                                {
                                    char chScanning = aggregatedData[chScanningIndex];
                                    if (chScanning == '/')
                                    {
                                        char chScanningLast = aggregatedData[chScanningIndex - 1];
                                        if (chScanningLast == '*')
                                        {
                                            break;
                                        }
                                    }
                                }


                                NssLexComment comment = new NssLexComment();
                                comment.m_CommentType = NssLexCommentType.BlockComment;
                                comment.m_Comment = aggregatedData.Substring(chBaseIndex + 2, chScanningIndex - chBaseIndex - 4);
                                tokens.Add(comment);
                                chBaseIndex = chScanningIndex + 1;
                                foundComment = true;
                            }

                            if (foundComment)
                            {
                                continue;
                            }
                        }
                    }
                }

                { // SEPARATORS
                    if (NssLexSeparator.Map.ContainsKey(ch))
                    {
                        tokens.Add(new NssLexSeparator { m_Separator = NssLexSeparator.Map[ch] });
                        ++chBaseIndex;
                        continue;
                    }
                }

                { // OPERATORS
                    if (NssLexOperator.Map.ContainsKey(ch))
                    {
                        tokens.Add(new NssLexOperator { m_Operator = NssLexOperator.Map[ch] });
                        ++chBaseIndex;
                        continue;
                    }
                }

                { // LITERALS
                    int chScanningIndex = chBaseIndex;

                    bool isString = ch == '"';
                    bool isNumber = char.IsNumber(ch);
                    if (isString || isNumber)
                    {
                        NssLexLiteral literal = new NssLexLiteral();

                        bool seenDecimalPlace = false;
                        while (++chScanningIndex < aggregatedData.Length)
                        {
                            char chScanning = aggregatedData[chScanningIndex];

                            if (isString)
                            {
                                // If we're a string, we just scan to the next ", except for escaped ones.
                                // There might be some weirdness with new lines here - but we'll just ignore them.
                                char chScanningLast = aggregatedData[chScanningIndex - 1];
                                if (chScanning == '"' && chScanningLast != '\\')
                                {
                                    literal.m_LiteralType = NssLexLiteralType.String;
                                    literal.m_Literal = aggregatedData.Substring(chBaseIndex + 1, chScanningIndex - chBaseIndex - 1);
                                    chBaseIndex = chScanningIndex + 1;
                                    break;
                                }
                            }
                            else
                            {
                                // If we're a number, we need to keep track of whether we've seen a decimal place,
                                // and scan until we're no longer a number or a decimal place.
                                if (chScanning == '.')
                                {
                                    seenDecimalPlace = true;
                                }
                                else if (!char.IsNumber(chScanning))
                                {
                                    literal.m_LiteralType = seenDecimalPlace ? NssLexLiteralType.Float : NssLexLiteralType.Int;
                                    literal.m_Literal = aggregatedData.Substring(chBaseIndex, chScanningIndex - chBaseIndex);
                                    chBaseIndex = chScanningIndex;
                                    break;
                                }
                            }
                        }

                        tokens.Add(literal);
                        continue;
                    }
                }

                { // KEYWORDS
                    if (tokens.Count == 0 || tokens.Last().GetType() == typeof(NssLexSeparator))
                    {
                        bool foundKeyword = false;

                        foreach (KeyValuePair<string, NssLexKeywords> kvp in NssLexKeyword.Map)
                        {
                            if (chBaseIndex + kvp.Key.Length >= aggregatedData.Length)
                            {
                                continue; // This would overrun us.
                            }

                            string strFromData = aggregatedData.Substring(chBaseIndex, kvp.Key.Length);
                            if (strFromData == kvp.Key)
                            {
                                tokens.Add(new NssLexKeyword { m_Keyword = kvp.Value });
                                chBaseIndex += kvp.Key.Length;
                                foundKeyword = true;
                                break;
                            }
                        }

                        if (foundKeyword)
                        {
                            continue;
                        }
                    }
                }

                { // IDENTIFIERS
                    int chScanningIndex = chBaseIndex;
                    while (++chScanningIndex < aggregatedData.Length)
                    {
                        char chScanning = aggregatedData[chScanningIndex];
                        if (NssLexSeparator.Map.ContainsKey(chScanning))
                        {
                            string literal = aggregatedData.Substring(chBaseIndex, chScanningIndex - chBaseIndex);
                            tokens.Add(new NssLexIdentifier { m_Identifier = literal });
                            chBaseIndex = chScanningIndex;
                            break;
                        }
                    }
                }
            }

            return 0;
        }
    }

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
        static void Main(string[] scripts)
        {
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
