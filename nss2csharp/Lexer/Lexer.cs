using System.Collections.Generic;
using System.Linq;

namespace nss2csharp
{
    public class NssLexicalAnalysis
    {
        public List<NssToken> Tokens { get; private set; }

        public int Analyse(IEnumerable<string> data)
        {
            // Combine everything into a flat string that we can process more easily.
            string aggregatedData = data.Aggregate((a, b) => a + b);

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
                            bool newLine = NssSeparator.Map.ContainsKey(chScanning) && NssSeparator.Map[chScanning] == NssSeparators.NewLine;

                            if (eof || newLine)
                            {
                                NssPreprocessor preprocessor = new NssPreprocessor();
                                preprocessor.m_PreprocessorType = NssPreprocessorType.Unknown;
                                int length = eof ? chScanningIndex - chBaseIndex : chScanningIndex - chBaseIndex - 1;
                                preprocessor.m_Data = aggregatedData.Substring(chBaseIndex + 1, length);
                                Tokens.Add(preprocessor);
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
                                    bool newLine = NssSeparator.Map.ContainsKey(chScanning) && NssSeparator.Map[chScanning] == NssSeparators.NewLine;

                                    if (eof || newLine)
                                    {
                                        NssComment comment = new NssComment();
                                        comment.m_CommentType = NssCommentType.LineComment;
                                        int length = eof ? chScanningIndex - chNextIndex : chScanningIndex - chNextIndex - 1;
                                        comment.m_Comment = aggregatedData.Substring(chNextIndex + 1, length);
                                        Tokens.Add(comment);
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


                                NssComment comment = new NssComment();
                                comment.m_CommentType = NssCommentType.BlockComment;
                                comment.m_Comment = aggregatedData.Substring(chBaseIndex + 2, chScanningIndex - chBaseIndex - 4);
                                Tokens.Add(comment);
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
                    if (NssSeparator.Map.ContainsKey(ch))
                    {
                        Tokens.Add(new NssSeparator { m_Separator = NssSeparator.Map[ch] });
                        ++chBaseIndex;
                        continue;
                    }
                }

                { // OPERATORS
                    if (NssOperator.Map.ContainsKey(ch))
                    {
                        Tokens.Add(new NssOperator { m_Operator = NssOperator.Map[ch] });
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
                        NssLiteral literal = new NssLiteral();

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
                                    literal.m_LiteralType = NssLiteralType.String;
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
                                    literal.m_LiteralType = seenDecimalPlace ? NssLiteralType.Float : NssLiteralType.Int;
                                    literal.m_Literal = aggregatedData.Substring(chBaseIndex, chScanningIndex - chBaseIndex);
                                    chBaseIndex = chScanningIndex;
                                    break;
                                }
                            }
                        }

                        Tokens.Add(literal);
                        continue;
                    }
                }

                { // KEYWORDS
                    if (Tokens.Count == 0 || Tokens.Last().GetType() == typeof(NssSeparator))
                    {
                        bool foundKeyword = false;

                        foreach (KeyValuePair<string, NssKeywords> kvp in NssKeyword.Map)
                        {
                            if (chBaseIndex + kvp.Key.Length >= aggregatedData.Length)
                            {
                                continue; // This would overrun us.
                            }

                            string strFromData = aggregatedData.Substring(chBaseIndex, kvp.Key.Length);
                            if (strFromData == kvp.Key)
                            {
                                Tokens.Add(new NssKeyword { m_Keyword = kvp.Value });
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
                        if (NssSeparator.Map.ContainsKey(chScanning))
                        {
                            string literal = aggregatedData.Substring(chBaseIndex, chScanningIndex - chBaseIndex);
                            Tokens.Add(new NssIdentifier { m_Identifier = literal });
                            chBaseIndex = chScanningIndex;
                            break;
                        }
                    }
                }
            }

            return 0;
        }
    }
}
