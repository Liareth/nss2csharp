using nss2csharp.Language;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nss2csharp.Lexer
{
    public class Lexer_Nss
    {
        public List<NssToken> Tokens { get; private set; }

        public List<NssLexDebugRange> DebugRanges { get; set; }

        public int Analyse(string data)
        {
            Tokens = new List<NssToken>();
            DebugRanges = new List<NssLexDebugRange>();

            { // Set up the debug data per line.
                int lineNum = 0;
                int cumulativeLen = 0;
                foreach (string line in data.Split('\n'))
                {
                    NssLexDebugRange range = new NssLexDebugRange();
                    range.Line = lineNum;
                    range.IndexStart = cumulativeLen;
                    range.IndexEnd = cumulativeLen + line.Length;
                    DebugRanges.Add(range);

                    lineNum = range.Line + 1;
                    cumulativeLen = range.IndexEnd + 1;
                }
            }

            int chBaseIndex = 0;
            while (chBaseIndex < data.Length)
            {
                int chBaseIndexLast = chBaseIndex;

                { // PREPROCESSOR
                    chBaseIndex = Preprocessor(chBaseIndex, data);
                    if (chBaseIndex != chBaseIndexLast) continue;
                }

                { // COMMENTS
                    chBaseIndex = Comment(chBaseIndex, data);
                    if (chBaseIndex != chBaseIndexLast) continue;
                }

                { // SEPARATORS
                    chBaseIndex = Separator(chBaseIndex, data);
                    if (chBaseIndex != chBaseIndexLast) continue;
                }

                { // OPERATORS
                    chBaseIndex = Operator(chBaseIndex, data);
                    if (chBaseIndex != chBaseIndexLast) continue;
                }

                { // LITERALS
                    chBaseIndex = Literal(chBaseIndex, data);
                    if (chBaseIndex != chBaseIndexLast) continue;
                }

                { // KEYWORDS
                    chBaseIndex = Keyword(chBaseIndex, data);
                    if (chBaseIndex != chBaseIndexLast) continue;
                }

                { // IDENTIFIERS
                    chBaseIndex = Identifier(chBaseIndex, data);
                    if (chBaseIndex != chBaseIndexLast) continue;
                }

                return 1;
            }

            return 0;
        }

        private int Preprocessor(int chBaseIndex, string data)
        {
            char ch = data[chBaseIndex];
            if (ch == '#')
            {
                // Just scan for a new line or eof, then add this in.
                int chScanningIndex = chBaseIndex;

                while (++chScanningIndex <= data.Length)
                {
                    bool eof = chScanningIndex >= data.Length - 1;

                    bool proceed = eof;
                    if (!proceed)
                    {
                        char chScanning = data[chScanningIndex];
                        proceed = NssSeparator.Map.ContainsKey(chScanning) &&
                            NssSeparator.Map[chScanning] == NssSeparators.NewLine;
                    }

                    if (proceed)
                    {
                        NssPreprocessor preprocessor = new NssPreprocessor();
                        preprocessor.m_PreprocessorType = NssPreprocessorType.Unknown;

                        int chStartIndex = chBaseIndex;
                        int chEndIndex = eof ? data.Length : chScanningIndex;

                        if (chStartIndex == chEndIndex)
                        {
                            preprocessor.m_Data = "";
                        }
                        else
                        {
                            preprocessor.m_Data = data.Substring(chStartIndex, chEndIndex - chStartIndex);
                        }

                        int chNewBaseIndex = chEndIndex;
                        AttachDebugData(preprocessor, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                        Tokens.Add(preprocessor);
                        chBaseIndex = chNewBaseIndex;
                        break;
                    }
                }
            }

            return chBaseIndex;
        }

        private int Comment(int chBaseIndex, string data)
        {
            char ch = data[chBaseIndex];
            if (ch == '/')
            {
                int chNextIndex = chBaseIndex + 1;
                if (chNextIndex < data.Length)
                {
                    char nextCh = data[chNextIndex];
                    if (nextCh == '/')
                    {
                        // Line comment - scan for end of line, and collect.
                        int chScanningIndex = chNextIndex;

                        while (++chScanningIndex <= data.Length)
                        {
                            bool eof = chScanningIndex >= data.Length - 1;

                            bool proceed = eof;
                            if (!proceed)
                            {
                                char chScanning = data[chScanningIndex];
                                proceed = NssSeparator.Map.ContainsKey(chScanning) &&
                                    NssSeparator.Map[chScanning] == NssSeparators.NewLine;
                            }

                            if (proceed)
                            {
                                NssComment comment = new NssComment();
                                comment.m_CommentType = NssCommentType.LineComment;

                                int chStartIndex = chNextIndex + 1;
                                int chEndIndex = eof ? data.Length : chScanningIndex;

                                if (chStartIndex == chEndIndex)
                                {
                                    comment.m_Comment = "";
                                }
                                else
                                {
                                    comment.m_Comment = data.Substring(chStartIndex, chEndIndex - chStartIndex);
                                }

                                int chNewBaseIndex = chEndIndex;
                                AttachDebugData(comment, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                                Tokens.Add(comment);
                                chBaseIndex = chNewBaseIndex;
                                break;
                            }
                        }
                    }
                    else if (nextCh == '*')
                    {
                        // Block comment - scan for the closing */, ignoring everything else.
                        bool terminated = false;
                        int chScanningIndex = chNextIndex + 1;
                        while (++chScanningIndex < data.Length)
                        {
                            char chScanning = data[chScanningIndex];
                            if (chScanning == '/')
                            {
                                char chScanningLast = data[chScanningIndex - 1];
                                if (chScanningLast == '*')
                                {
                                    terminated = true;
                                    break;
                                }
                            }
                        }

                        bool eof = chScanningIndex >= data.Length - 1;

                        NssComment comment = new NssComment();
                        comment.m_CommentType = NssCommentType.BlockComment;
                        comment.m_Terminated = terminated;

                        int chStartIndex = chBaseIndex + 2;
                        int chEndIndex = !terminated && eof ? data.Length : chScanningIndex + (terminated ? -1 : 0);
                        comment.m_Comment = data.Substring(chStartIndex, chEndIndex - chStartIndex);

                        int chNewBaseIndex = eof ? data.Length : chScanningIndex + 1;
                        AttachDebugData(comment, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                        Tokens.Add(comment);
                        chBaseIndex = chNewBaseIndex;
                    }
                }
            }

            return chBaseIndex;
        }

        private int Separator(int chBaseIndex, string data)
        {
            char ch = data[chBaseIndex];

            if (NssSeparator.Map.ContainsKey(ch))
            {
                NssSeparator separator = new NssSeparator();
                separator.m_Separator = NssSeparator.Map[ch];

                int chNewBaseIndex = chBaseIndex + 1;
                AttachDebugData(separator, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                Tokens.Add(separator);
                chBaseIndex = chNewBaseIndex;
            }

            return chBaseIndex;
        }

        private int Operator(int chBaseIndex, string data)
        {
            char ch = data[chBaseIndex];

            if (NssOperator.Map.ContainsKey(ch))
            {
                NssOperator op = new NssOperator();
                op.m_Operator = NssOperator.Map[ch];

                int chNewBaseIndex = chBaseIndex + 1;
                AttachDebugData(op, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                Tokens.Add(op);
                chBaseIndex = chNewBaseIndex;
            }

            return chBaseIndex;
        }

        private int Literal(int chBaseIndex, string data)
        {
            char ch = data[chBaseIndex];
            bool isString = ch == '"';
            bool isNumber = char.IsNumber(ch);
            if (isString || isNumber)
            {
                NssLiteral literal = null;

                bool seenDecimalPlace = false;
                int chScanningIndex = chBaseIndex;
                while (++chScanningIndex < data.Length)
                {
                    char chScanning = data[chScanningIndex];

                    if (isString)
                    {
                        // If we're a string, we just scan to the next ", except for escaped ones.
                        // There might be some weirdness with new lines here - but we'll just ignore them.
                        char chScanningLast = data[chScanningIndex - 1];
                        if (chScanning == '"' && chScanningLast != '\\')
                        {
                            literal = new NssLiteral();
                            literal.m_LiteralType = NssLiteralType.String;

                            int chStartIndex = chBaseIndex;
                            int chEndIndex = chScanningIndex + 1;

                            if (chStartIndex == chEndIndex)
                            {
                                literal.m_Literal = "";
                            }
                            else
                            {
                                literal.m_Literal = data.Substring(chStartIndex, chEndIndex - chStartIndex);
                            }

                            Tokens.Add(literal);
                            int chNewBaseIndex = chEndIndex;
                            AttachDebugData(literal, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                            chBaseIndex = chNewBaseIndex;
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
                        else if (!char.IsNumber(chScanning) && (!seenDecimalPlace || (seenDecimalPlace && chScanning != 'f')))
                        {
                            literal = new NssLiteral();
                            literal.m_LiteralType = seenDecimalPlace ? NssLiteralType.Float : NssLiteralType.Int;

                            int chStartIndex = chBaseIndex;
                            int chEndIndex = chScanningIndex;
                            literal.m_Literal = data.Substring(chStartIndex, chEndIndex - chStartIndex);

                            int chNewBaseIndex = chScanningIndex;
                            AttachDebugData(literal, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                            Tokens.Add(literal);
                            chBaseIndex = chNewBaseIndex;
                            break;
                        }
                    }
                }
            }

            return chBaseIndex;
        }

        private int Keyword(int chBaseIndex, string data)
        {
            char ch = data[chBaseIndex];

            if (Tokens.Count == 0 ||
                Tokens.Last().GetType() == typeof(NssSeparator) ||
                Tokens.Last().GetType() == typeof(NssOperator))
            {
                foreach (KeyValuePair<string, NssKeywords> kvp in NssKeyword.Map)
                {
                    if (chBaseIndex + kvp.Key.Length >= data.Length)
                    {
                        continue; // This would overrun us.
                    }

                    string strFromData = data.Substring(chBaseIndex, kvp.Key.Length);
                    if (strFromData == kvp.Key)
                    {
                        // We're matched a keyword, e.g. 'int ', but we might have, e.g. 'int integral', and the
                        // 'integral' is an identifier. So let's only accept a keyword if the character proceeding it
                        // is whitespace.
                        // Note - this is only true for some keywords, namely keywords that declare types.
                        // Others don't care about that.

                        List<NssKeywords> keywordsThatCareAboutSpaces = new List<NssKeywords>
                        {
                            NssKeywords.Case,
                            NssKeywords.Const,
                            NssKeywords.Void,
                            NssKeywords.Int,
                            NssKeywords.Float,
                            NssKeywords.String,
                            NssKeywords.Struct,
                            NssKeywords.Object,
                            NssKeywords.Location,
                            NssKeywords.Vector,
                            NssKeywords.ItemProperty,
                            NssKeywords.Effect
                        };

                        int chNextAlongIndex = chBaseIndex + kvp.Key.Length;
                        bool accept = !keywordsThatCareAboutSpaces.Contains(kvp.Value);

                        if (!accept)
                        {
                            char chNextAlong = data[chNextAlongIndex];

                            if (NssSeparator.Map.ContainsKey(chNextAlong))
                            {
                                NssSeparators sep = NssSeparator.Map[chNextAlong];
                                if (sep == NssSeparators.Space || sep == NssSeparators.Tab)
                                {
                                    accept = true;
                                }
                            }
                        }

                        if (accept)
                        {
                            NssKeyword keyword = new NssKeyword();
                            keyword.m_Keyword = kvp.Value;

                            int chNewBaseIndex = chNextAlongIndex;
                            AttachDebugData(keyword, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                            Tokens.Add(keyword);
                            chBaseIndex = chNewBaseIndex;
                            break;
                        }
                    }
                }
            }

            return chBaseIndex;
        }

        private int Identifier(int chBaseIndex, string data)
        {
            char ch = data[chBaseIndex];

            int chScanningIndex = chBaseIndex;
            bool eof;

            do
            {
                eof = chScanningIndex >= data.Length - 1;
                char chScanning = data[chScanningIndex];

                bool hasOperator = NssOperator.Map.ContainsKey(chScanning); // An identifier ends at the first sight of an operator.
                bool hasSeparator = NssSeparator.Map.ContainsKey(chScanning);
                if (eof || hasSeparator || hasOperator)
                {
                    NssIdentifier identifier = new NssIdentifier();

                    int chStartIndex = chBaseIndex;
                    int chEndIndex = chScanningIndex + (eof ? 1 : 0);
                    identifier.m_Identifier = data.Substring(chStartIndex, chEndIndex - chStartIndex);

                    int chNewBaseIndex = chScanningIndex + (eof ? 1 : 0);
                    AttachDebugData(identifier, DebugRanges, chBaseIndex, chNewBaseIndex - 1);

                    Tokens.Add(identifier);
                    chBaseIndex = chNewBaseIndex;
                    break;
                }

                ++chScanningIndex;
            } while (chScanningIndex < data.Length);

            return chBaseIndex;
        }

        public struct NssLexDebugRange
        {
            public int Line { get; set; }
            public int IndexStart { get; set; }
            public int IndexEnd { get; set; }
        }

        public struct NssLexDebugInfo
        {
            public int LineStart { get; set; }
            public int LineEnd { get; set; }
            public int ColumnStart { get; set; }
            public int ColumnEnd { get; set; }
        }

        private void AttachDebugData(NssToken token, List<NssLexDebugRange> debugRanges, int indexStart, int indexEnd)
        {
            NssLexDebugInfo debugInfo = new NssLexDebugInfo();

            for (int i = 0; i < debugRanges.Count; ++i)
            {
                NssLexDebugRange startDebugRange = debugRanges[i];
                NssLexDebugRange endDebugRange = debugRanges[i];

                if (indexStart >= startDebugRange.IndexStart && indexStart < startDebugRange.IndexEnd)
                {
                    int endIndex;

                    for (endIndex = i; endIndex < debugRanges.Count; ++endIndex)
                    {
                        endDebugRange = debugRanges[endIndex];
                        if (indexStart >= endDebugRange.IndexStart && indexStart < endDebugRange.IndexEnd)
                        {
                            break;
                        }
                    }

                    debugInfo.LineStart = i;
                    debugInfo.LineEnd = endIndex;
                    debugInfo.ColumnStart = indexStart - startDebugRange.IndexStart;
                    debugInfo.ColumnEnd = indexEnd - endDebugRange.IndexStart;
                }
            }

            token.UserData = debugInfo;
        }
    }
}
