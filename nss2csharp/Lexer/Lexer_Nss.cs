using System;
using System.Collections.Generic;
using System.Linq;

namespace nss2csharp
{
    public class Lexer_Nss
    {
        public List<NssToken> Tokens { get; private set; }

        public int Analyse(string data)
        {
            Tokens = new List<NssToken>();

            List<NssLexDebugRange> debugRanges = new List<NssLexDebugRange>();

            { // Set up the debug data per line.
                int lineNum = 0;
                int cumulativeLen = 0;
                foreach (string line in data.Split('\n'))
                {
                    NssLexDebugRange range = new NssLexDebugRange();
                    range.Line = lineNum;
                    range.IndexStart = cumulativeLen;
                    range.IndexEnd = cumulativeLen + line.Length;
                    debugRanges.Add(range);

                    lineNum = range.Line + 1;
                    cumulativeLen = range.IndexEnd + 1;
                }
            }

            int chBaseIndex = 0;
            while (chBaseIndex < data.Length)
            {
                char ch = data[chBaseIndex];

                { // PREPROCESSOR
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
                                AttachDebugData(preprocessor, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                                Tokens.Add(preprocessor);
                                chBaseIndex = chNewBaseIndex;
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
                        if (chNextIndex < data.Length)
                        {
                            bool foundComment = false;

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
                                        AttachDebugData(comment, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                                        Tokens.Add(comment);
                                        chBaseIndex = chNewBaseIndex;
                                        break;
                                    }
                                }

                                foundComment = true;
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
                                AttachDebugData(comment, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                                Tokens.Add(comment);
                                chBaseIndex = chNewBaseIndex;
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
                    if (NssSeparator.Map.ContainsKey(ch))
                    {
                        NssSeparator separator = new NssSeparator();
                        separator.m_Separator = NssSeparator.Map[ch];

                        int chNewBaseIndex = chBaseIndex + 1;
                        AttachDebugData(separator, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                        Tokens.Add(separator);
                        chBaseIndex = chNewBaseIndex;
                        continue;
                    }
                }

                { // OPERATORS
                    if (NssOperator.Map.ContainsKey(ch))
                    {
                        NssOperator op = new NssOperator();
                        op.m_Operator = NssOperator.Map[ch];

                        int chNewBaseIndex = chBaseIndex + 1;
                        AttachDebugData(op, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                        Tokens.Add(op);
                        chBaseIndex = chNewBaseIndex;
                        continue;
                    }
                }

                { // LITERALS
                    int chScanningIndex = chBaseIndex;

                    bool isString = ch == '"';
                    bool isNumber = char.IsNumber(ch);
                    if (isString || isNumber)
                    {
                        NssLiteral literal = null;

                        bool seenDecimalPlace = false;
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

                                    int chNewBaseIndex = chEndIndex;
                                    AttachDebugData(literal, debugRanges, chBaseIndex, chNewBaseIndex - 1);

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
                                else if (!char.IsNumber(chScanning))
                                {
                                    literal = new NssLiteral();
                                    literal.m_LiteralType = seenDecimalPlace ? NssLiteralType.Float : NssLiteralType.Int;

                                    int chStartIndex = chBaseIndex;
                                    int chEndIndex = chScanningIndex;
                                    literal.m_Literal = data.Substring(chStartIndex, chEndIndex - chStartIndex);

                                    int chNewBaseIndex = chScanningIndex;
                                    AttachDebugData(literal, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                                    chBaseIndex = chNewBaseIndex;
                                    break;
                                }
                            }
                        }

                        // The literal can be null here if we never entered the scanning loop -
                        // e.g. if we're at eof.
                        if (literal != null)
                        {
                            Tokens.Add(literal);
                            continue;
                        }
                    }
                }

                { // KEYWORDS
                    if (Tokens.Count == 0 || Tokens.Last().GetType() == typeof(NssSeparator))
                    {
                        bool foundKeyword = false;

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
                                int chNextAlongIndex = chBaseIndex + kvp.Key.Length;
                                char chNextAlong = data[chNextAlongIndex];

                                if (NssSeparator.Map.ContainsKey(chNextAlong))
                                {
                                    NssSeparators sep = NssSeparator.Map[chNextAlong];
                                    if (sep == NssSeparators.Space || sep == NssSeparators.Tab)
                                    {
                                        NssKeyword keyword = new NssKeyword();
                                        keyword.m_Keyword = kvp.Value;

                                        int chNewBaseIndex = chNextAlongIndex;
                                        AttachDebugData(keyword, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                                        Tokens.Add(keyword);
                                        chBaseIndex = chNewBaseIndex;
                                        foundKeyword = true;
                                        break;
                                    }
                                }
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
                    bool eof;

                    do
                    {
                        eof = chScanningIndex >= data.Length - 1;

                        char chScanning = data[chScanningIndex];
                        bool hasSeparator = NssSeparator.Map.ContainsKey(chScanning);

                        if (eof || hasSeparator)
                        {
                            NssIdentifier identifier = new NssIdentifier();

                            int chStartIndex = chBaseIndex;
                            int chEndIndex = chScanningIndex + (eof ? 1 : 0);
                            identifier.m_Identifier = data.Substring(chStartIndex, chEndIndex - chStartIndex);

                            int chNewBaseIndex = chScanningIndex + (eof ? 1 : 0);
                            AttachDebugData(identifier, debugRanges, chBaseIndex, chNewBaseIndex - 1);

                            Tokens.Add(identifier);
                            chBaseIndex = chNewBaseIndex;
                            break;
                        }

                        ++chScanningIndex;
                    } while (chScanningIndex < data.Length);
                }
            }

            return 0;
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
