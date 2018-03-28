using System.Collections.Generic;

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
        public NssNode m_Parent;
        public List<NssNode> m_Children;
        public List<NssToken> m_Tokens; // The tokens that originally comprised this node.
    }

    public struct NssCompilationUnitMetadata
    {
        public string m_Name;
    }

    public class NssCompilationUnit : NssNode
    {
        public NssCompilationUnitMetadata m_Metadata;
    }

    public class Parser_Nss
    {
        public NssCompilationUnit CompilationUnit { get; private set; }

        public List<NssToken> Tokens { get; private set; }

        public List<string> Errors { get; private set; }

        public int Parse(string name, List<NssToken> tokens)
        {
            CompilationUnit = new NssCompilationUnit();
            Tokens = tokens;
            Errors = new List<string>();

            { // METADATA
                NssCompilationUnitMetadata metadata = new NssCompilationUnitMetadata();
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

        private int Parse(NssNode parent, ref int baseIndexRef)
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

        private void ConstructPreprocessor(NssNode parent, ref int baseIndexRef)
        {
        }

        private void ConstructComment(NssNode parent, ref int baseIndexRef)
        {
        }

        private void ConstructFunctionDeclaration(NssNode parent, ref int baseIndexRef)
        {
        }

        private void ConstructFunctionDefinition(NssNode parent, ref int baseIndexRef)
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

        private void ConstructUnassignedLvalue(NssNode parent, ref int baseIndexRef)
        {
        }

        private void ConstructAssignedLvalue(NssNode parent, ref int baseIndexRef)
        {
        }

        private void ConstructBlock_r(NssNode parent, ref int baseIndexRef)
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

        private int TraverseNextToken(out NssToken token, ref int baseIndexRef, bool skipWhitespace = false)
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
                    if (sep != null && (sep.m_Separator == NssSeparators.Tab ||sep.m_Separator == NssSeparators.Space))
                    {
                        ret = null;
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
