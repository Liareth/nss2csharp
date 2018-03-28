using nss2csharp.Language;
using nss2csharp.Lexer;
using System.Collections.Generic;
using System.Linq;

namespace nss2csharp.Parser
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

    public class Node
    {
        public Node m_Parent;
        public List<Node> m_Children;
        public List<NssToken> m_Tokens; // The tokens that originally comprised this node.
    }

    public struct CompilationUnitMetadata
    {
        public string m_Name;
    }

    public class CompilationUnit : Node
    {
        public CompilationUnitMetadata m_Metadata;
    }

    public abstract class Preprocessor : Node
    { }

    public class UnknownPreprocessor : Preprocessor
    {
        public string m_Value;
    }

    public abstract class Comment : Node
    { }

    public class LineComment : Comment
    {
        public string m_Comment;
    }

    public class BlockComment : Comment
    {
        public List<string> m_CommentLines;
    }

    public class Parser_Nss
    {
        public CompilationUnit CompilationUnit { get; private set; }

        public List<NssToken> Tokens { get; private set; }

        public List<string> Errors { get; private set; }

        public int Parse(string name, List<NssToken> tokens)
        {
            CompilationUnit = new CompilationUnit();
            Tokens = tokens;
            Errors = new List<string>();

            { // METADATA
                CompilationUnitMetadata metadata = new CompilationUnitMetadata();
                metadata.m_Name = name;
                CompilationUnit.m_Metadata = metadata;
            }

            for (int baseIndex = 0; baseIndex < tokens.Count; ++baseIndex)
            {
                int baseIndexLast = baseIndex;

                int err = Parse(CompilationUnit, ref baseIndex);
                if (err != 0)
                {
                    return err;
                }
            }

            return 0;
        }

        private int Parse(Node parent, ref int baseIndexRef)
        {
            int baseIndexLast = baseIndexRef;

            // This is the root scope.
            //
            // Here it's valid to have either ...
            // - Preprocessor commands
            // - Comments
            // - Function declarations
            // - Function definitions
            // - Variables (constant or global)

            { // PREPROCESSOR
                ConstructPreprocessor(parent, ref baseIndexRef);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            { // COMMENT
                ConstructComment(parent, ref baseIndexRef);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            { // FUNCTION DECLARATION
                ConstructFunctionDeclaration(parent, ref baseIndexRef);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            { // FUNCTION DEFINITION
                ConstructFunctionDefinition(parent, ref baseIndexRef);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            { // VARIABLE
                ConstructAssignedLvalue(parent, ref baseIndexRef);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            NssToken token;

            if (TraverseNextToken(out token, ref baseIndexRef) == 0)
            {
                ReportTokenError(token, "Unrecognised / unhandled token");
            }
            else
            {
                Errors.Add("Unknown parser error.");
            }

            return 1;
        }

        private Preprocessor ConstructPreprocessor(Node parent, ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;
            NssToken token;

            int err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssPreprocessor)) return null;

            baseIndexRef = baseIndex;

            return new UnknownPreprocessor
            {
                m_Parent = parent,
                m_Tokens = new List<NssToken> { token },
                m_Value = ((NssPreprocessor)token).m_Data
            };
        }

        private Comment ConstructComment(Node parent, ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;
            NssToken token;

            int err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssComment)) return null;
            NssComment commentToken = (NssComment)token;

            Comment comment;

            if (commentToken.m_CommentType == NssCommentType.LineComment)
            {
                comment = new LineComment { m_Comment = commentToken.m_Comment };
            }
            else
            {
                if (!commentToken.m_Terminated) return null;
                comment = new BlockComment { m_CommentLines = commentToken.m_Comment.Split('\n').ToList() };
            }

            comment.m_Parent = parent;
            comment.m_Tokens = new List<NssToken> { token };

            baseIndexRef = baseIndex;

            return comment;
        }

        private void ConstructFunctionDeclaration(Node parent, ref int baseIndexRef)
        {
        }

        private void ConstructFunctionDefinition(Node parent, ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;
            NssToken token;

            NssKeyword returnType;
            NssIdentifier structName;
            NssIdentifier functionName;

            int err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return;
            returnType = (NssKeyword)token;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssIdentifier)) return;
            functionName = (NssIdentifier)token;

            if (returnType.m_Keyword == NssKeywords.Struct)
            {
                structName = functionName; // Struct name comes first, so swap it.
                err = TraverseNextToken(out token, ref baseIndex);
                if (err != 0 || token.GetType() != typeof(NssIdentifier)) return;
                functionName = (NssIdentifier)token;
            }

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return;
            if (((NssSeparator)token).m_Separator != NssSeparators.OpenParen) return;

            while (true)
            {
                int baseIndexVariable = baseIndex;
                ConstructUnassignedLvalue(parent, ref baseIndex);
                if (baseIndexVariable == baseIndex) break;
            }

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return;
            if (((NssSeparator)token).m_Separator != NssSeparators.CloseParen) return;

            int baseIndexBlock = baseIndex;
            ConstructBlock_r(parent, ref baseIndex);
            if (baseIndexBlock == baseIndex) return;

            baseIndexRef = baseIndex;
        }

        private void ConstructUnassignedLvalue(Node parent, ref int baseIndexRef)
        {
        }

        private void ConstructAssignedLvalue(Node parent, ref int baseIndexRef)
        {
        }

        private void ConstructBlock_r(Node parent, ref int baseIndexRef)
        {
        }

        private void ReportTokenError(NssToken token, string error)
        {
            Errors.Add(error);
            Errors.Add(string.Format("On Token type {0}", token.GetType().Name));

            if (token.UserData != null)
            {
                Lexer_Nss.NssLexDebugInfo debugInfo = (Lexer_Nss.NssLexDebugInfo)token.UserData;
                Errors.Add(string.Format("At line {0}:{1} to line {2}:{3}.",
                    debugInfo.LineStart, debugInfo.ColumnStart,
                    debugInfo.LineEnd, debugInfo.ColumnEnd));
            }
        }

        private int TraverseNextToken(out NssToken token, ref int baseIndexRef, bool skipWhitespace = true)
        {
            NssToken ret = null;

            int baseIndex = baseIndexRef;

            while (ret == null)
            {
                if (baseIndex >= Tokens.Count)
                {
                    token = null;
                    return 1;
                }

                ret = Tokens[baseIndex];

                if (skipWhitespace)
                {
                    NssSeparator sep = ret as NssSeparator;
                    if (sep != null && (
                        sep.m_Separator == NssSeparators.Tab ||
                        sep.m_Separator == NssSeparators.Space ||
                        sep.m_Separator == NssSeparators.NewLine ))
                    {
                        ret = null;
                        ++baseIndex;
                        continue;
                    }
                }
            }

            baseIndexRef = ++baseIndex;
            token = ret;
            return 0;
        }
    }
}
