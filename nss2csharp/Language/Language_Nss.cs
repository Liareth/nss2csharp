using System;
using System.Collections.Generic;

namespace nss2csharp.Language
{
    public class NssToken : IToken
    {
        public object UserData { get; set; } = null;
    }

    public class Language_Nss : ILanguage
    {
        public string StringFromToken(IToken token)
        {
            NssToken nssToken = token as NssToken;
            return nssToken == null ? null : nssToken.ToString();
        }
    }

    public enum NssKeywords
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
        Effect,
        Talent,
        Action,
        Event,
        ObjectInvalid,
        ObjectSelf,
        Default
    }

    public class NssKeyword : NssToken
    {
        public override string ToString()
        {
            foreach (KeyValuePair<string, NssKeywords> kvp in Map)
            {
                if (kvp.Value == m_Keyword)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        public static Dictionary<string, NssKeywords> Map = new Dictionary<string, NssKeywords>
        {
            { "if",             NssKeywords.If },
            { "else",           NssKeywords.Else },
            { "for",            NssKeywords.For },
            { "while",          NssKeywords.While },
            { "do",             NssKeywords.Do },
            { "switch",         NssKeywords.Switch },
            { "break",          NssKeywords.Break },
            { "return",         NssKeywords.Return },
            { "case",           NssKeywords.Case },
            { "const",          NssKeywords.Const },
            { "void",           NssKeywords.Void },
            { "int",            NssKeywords.Int },
            { "float",          NssKeywords.Float },
            { "string",         NssKeywords.String },
            { "struct",         NssKeywords.Struct },
            { "object",         NssKeywords.Object },
            { "location",       NssKeywords.Location },
            { "vector",         NssKeywords.Vector },
            { "itemproperty",   NssKeywords.ItemProperty },
            { "effect",         NssKeywords.Effect },
            { "talent",         NssKeywords.Talent },
            { "action",         NssKeywords.Action },
            { "event",          NssKeywords.Event },
            { "OBJECT_INVALID", NssKeywords.ObjectInvalid },
            { "OBJECT_SELF",    NssKeywords.ObjectSelf },
            { "default",        NssKeywords.Default },
        };

        public NssKeywords m_Keyword;
    }

    public class NssIdentifier : NssToken
    {
        public override string ToString()
        {
            return m_Identifier;
        }

        public string m_Identifier;
    }

    public enum NssSeparators
    {
        Space,
        NewLine,
        OpenParen,
        CloseParen,
        OpenCurlyBrace,
        CloseCurlyBrace,
        Semicolon,
        Tab,
        Comma,
        OpenSquareBracket,
        CloseSquareBracket
    }

    public class NssSeparator : NssToken
    {
        public override string ToString()
        {
            foreach (KeyValuePair<char, NssSeparators> kvp in Map)
            {
                if (kvp.Value == m_Separator)
                {
                    return kvp.Key.ToString();
                }
            }

            return null;
        }

        public static Dictionary<char, NssSeparators> Map = new Dictionary<char, NssSeparators>
        {
            { ' ',  NssSeparators.Space },
            { '\n', NssSeparators.NewLine },
            { '(',  NssSeparators.OpenParen },
            { ')',  NssSeparators.CloseParen },
            { '{',  NssSeparators.OpenCurlyBrace },
            { '}',  NssSeparators.CloseCurlyBrace },
            { ';',  NssSeparators.Semicolon },
            { '\t', NssSeparators.Tab },
            { ',',  NssSeparators.Comma },
            { '[',  NssSeparators.OpenSquareBracket },
            { ']',  NssSeparators.CloseSquareBracket }
        };

        public NssSeparators m_Separator;
    }

    public enum NssOperators
    {
        Addition,
        Subtraction,
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

    public class NssOperator : NssToken
    {
        public override string ToString()
        {
            foreach (KeyValuePair<char, NssOperators> kvp in Map)
            {
                if (kvp.Value == m_Operator)
                {
                    return kvp.Key.ToString();
                }
            }

            return null;
        }

        public static Dictionary<char, NssOperators> Map = new Dictionary<char, NssOperators>
        {
            { '+',  NssOperators.Addition },
            { '-',  NssOperators.Subtraction },
            { '/',  NssOperators.Division },
            { '*',  NssOperators.Multiplication },
            { '%',  NssOperators.Modulo },
            { '&',  NssOperators.And },
            { '|',  NssOperators.Or },
            { '!',  NssOperators.Not },
            { '~',  NssOperators.Inversion },
            { '>',  NssOperators.GreaterThan },
            { '<',  NssOperators.LessThan },
            { '=',  NssOperators.Equals },
            { '?',  NssOperators.TernaryQuestionMark },
            { ':',  NssOperators.TernaryColon },
        };

        public NssOperators m_Operator;
    }

    public enum NssLiteralType
    {
        Int,
        Float,
        String,
    }

    public class NssLiteral : NssToken
    {
        public override string ToString()
        {
            return m_Literal;
        }

        public NssLiteralType m_LiteralType;
        public string m_Literal;
    }

    public enum NssCommentType
    {
        LineComment,
        BlockComment
    }

    public class NssComment : NssToken
    {
        public override string ToString()
        {
            if (m_CommentType == NssCommentType.LineComment)
            {
                return "//" + m_Comment;
            }
            else if (m_CommentType == NssCommentType.BlockComment)
            {
                return "/*" + m_Comment + (m_Terminated ? "*/" : "");
            }

            return null;
        }

        public NssCommentType m_CommentType;
        public string m_Comment;
        public bool m_Terminated; // If a block style comment, whether it was actually terminated
    }

    public enum NssPreprocessorType
    {
        Unknown
    }

    public class NssPreprocessor : NssToken
    {
        public override string ToString()
        {
            return m_Data;
        }

        public NssPreprocessorType m_PreprocessorType;
        public string m_Data;
    }
}
