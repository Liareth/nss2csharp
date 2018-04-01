using nss2csharp.Language;
using nss2csharp.Lexer;
using System.Collections.Generic;
using System.Linq;

namespace nss2csharp.Parser
{
    public class Parser_Nss
    {
        public CompilationUnit CompilationUnit { get; private set; }

        public List<NssToken> Tokens { get; private set; }

        public List<string> Errors { get; private set; }

        public int Parse(string name, string[] sourceData, List<NssToken> tokens)
        {
            CompilationUnit = new CompilationUnit();
            Tokens = tokens;
            Errors = new List<string>();

            { // METADATA
                CompilationUnitMetadata metadata = new CompilationUnitMetadata();
                metadata.m_Name = name;
                CompilationUnit.m_Metadata = metadata;
            }

            { // DEBUG INFO
                CompilationUnitDebugInfo debugInfo = new CompilationUnitDebugInfo();
                debugInfo.m_SourceData = sourceData;
                CompilationUnit.m_DebugInfo = debugInfo;
            }

            for (int baseIndex = 0; baseIndex < tokens.Count; ++baseIndex)
            {
                int baseIndexLast = baseIndex;

                int err = Parse(ref baseIndex);
                if (err != 0)
                {
                    return err;
                }
            }

            return 0;
        }

        private int Parse(ref int baseIndexRef)
        {
            int baseIndexLast = baseIndexRef;

            // This is the root scope.
            //
            // Here it's valid to have either ...
            //
            // - Preprocessor commands
            // - Functions (declaration or implementation)
            // - Variables (constant or global)

            { // PREPROCESSOR
                Node node = ConstructPreprocessor(ref baseIndexRef);
                if (node != null) CompilationUnit.m_Nodes.Add(node);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            { // FUNCTION
                Node node = ConstructFunction(ref baseIndexRef);
                if (node != null) CompilationUnit.m_Nodes.Add(node);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            { // VARIABLES
                Node node = ConstructLvalueDecl(ref baseIndexRef);
                if (node != null) CompilationUnit.m_Nodes.Add(node);
                if (baseIndexLast != baseIndexRef) return 0;
            }

            if (TraverseNextToken(out NssToken token, ref baseIndexRef) == 0)
            {
                ReportTokenError(token, "Unrecognised / unhandled token");
                return 1;
            }

            return 0;
        }

        private Preprocessor ConstructPreprocessor(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssPreprocessor)) return null;

            baseIndexRef = baseIndex;

            return new UnknownPreprocessor { m_Value = ((NssPreprocessor)token).m_Data };
        }

        private Comment ConstructComment(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
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

            baseIndexRef = baseIndex;
            return comment;
        }

        private Type ConstructType(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;

            Type ret = null;

            switch (((NssKeyword)token).m_Keyword)
            {
                case NssKeywords.Void: ret = new VoidType(); break;
                case NssKeywords.Int: ret = new IntType(); break;
                case NssKeywords.Float: ret = new FloatType(); break;
                case NssKeywords.String: ret = new StringType(); break;
                case NssKeywords.Struct:
                {
                    StructType str = new StructType();

                    err = TraverseNextToken(out token, ref baseIndex);
                    if (err != 0 || token.GetType() != typeof(NssIdentifier)) return null;

                    str.m_TypeName = ((NssIdentifier)token).m_Identifier;
                    ret = str;

                    break;
                }

                case NssKeywords.Object: ret = new ObjectType(); break;
                case NssKeywords.Location: ret = new LocationType(); break;
                case NssKeywords.Vector: ret = new VectorType(); break;
                case NssKeywords.ItemProperty: ret = new ItemPropertyType(); break;
                case NssKeywords.Effect: ret = new EffectType(); break;
                default:
                    return null;
            }

            baseIndexRef = baseIndex;
            return ret;
        }

        private Function ConstructFunction(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            Type returnType = ConstructType(ref baseIndex);
            if (returnType == null) return null;

            Lvalue functionName = ConstructLvalue(ref baseIndex);
            if (functionName == null) return null;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.OpenParen) return null;

            List<FunctionParameter> parameters = new List<FunctionParameter>();

            while (true)
            {
                err = TraverseNextToken(out token, ref baseIndex);
                if (err != 0) return null;

                // Terminate the loop if we're a close paren, or step back if not so we can continue our scan.
                if (token.GetType() == typeof(NssSeparator) && ((NssSeparator)token).m_Separator == NssSeparators.CloseParen) break;
                else --baseIndex;

                Type paramType = ConstructType(ref baseIndex);
                if (paramType == null) return null;

                Lvalue paramName = ConstructLvalue(ref baseIndex);
                if (paramName == null) return null;

                err = TraverseNextToken(out token, ref baseIndex);
                if (err != 0) return null;

                FunctionParameter param = null;

                // Default value.
                if (token.GetType() == typeof(NssOperator))
                {
                    if (((NssOperator)token).m_Operator != NssOperators.Equals) return null;

                    Value defaultVal = ConstructRvalue(ref baseIndex);
                    if (defaultVal == null)
                    {
                        defaultVal = ConstructLvalue(ref baseIndex);
                        if (defaultVal == null) return null;
                    }

                    param = new FunctionParameterWithDefault { m_Default = defaultVal };
                    param.m_Type = paramType;
                    param.m_Name = paramName;
                    parameters.Add(param);

                    err = TraverseNextToken(out token, ref baseIndex);
                    if (err != 0) return null;

                    // If we're not a comman, just step back so the loop above can handle us.
                    if (token.GetType() == typeof(NssSeparator) && (((NssSeparator)token).m_Separator != NssSeparators.Comma)) --baseIndex;

                    continue;
                }
                // Close paren or comma

                if (token.GetType() == typeof(NssSeparator))
                {
                    NssSeparator sepParams = (NssSeparator)token;

                    if (sepParams.m_Separator == NssSeparators.CloseParen ||
                        sepParams.m_Separator == NssSeparators.Comma)
                    {
                        param = new FunctionParameter();
                        param.m_Type = paramType;
                        param.m_Name = paramName;
                        parameters.Add(param);

                        if (sepParams.m_Separator == NssSeparators.CloseParen) break;
                    }
                    else
                    {
                        return null;
                    }

                    continue;
                }

                return null;
            }

            Function ret = null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;

            if (((NssSeparator)token).m_Separator == NssSeparators.Semicolon)
            {
                ret = new FunctionDeclaration();
            }
            else if (((NssSeparator)token).m_Separator == NssSeparators.OpenCurlyBrace)
            {
                --baseIndex; // Step base index back for the block function

                Block block = ConstructBlock_r(ref baseIndex);
                if (block == null) return null;

                ret = new FunctionImplementation { m_Block = block };
            }
            else
            {
                return null;
            }

            ret.m_Parameters = parameters;

            baseIndexRef = baseIndex;
            return ret;
        }

        private Lvalue ConstructLvalue(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssIdentifier)) return null;

            Lvalue ret = new Lvalue();
            ret.m_Identifier = ((NssIdentifier)token).m_Identifier;

            baseIndexRef = baseIndex;
            return ret;
        }

        private Rvalue ConstructRvalue(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssLiteral)) return null;

            Rvalue ret = null;

            NssLiteral lit = (NssLiteral)token;
            switch (lit.m_LiteralType)
            {
                case NssLiteralType.Int:
                {
                    if (!int.TryParse(lit.m_Literal, out int value)) return null;
                    ret = new IntLiteral { m_Value = value };
                    break;
                }

                case NssLiteralType.Float:
                {
                    if (!float.TryParse(lit.m_Literal, out float value)) return null;
                    ret = new FloatLiteral { m_Value = value };
                    break;
                }

                case NssLiteralType.String:
                    ret = new StringLiteral { m_Value = lit.m_Literal };
                    break;

                default: return null;
            }

            baseIndexRef = baseIndex;
            return ret;
        }

        private LvalueDecl ConstructLvalueDecl(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            // Constness
            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0) return null;
            bool constness = token.GetType() == typeof(NssKeyword) && ((NssKeyword)token).m_Keyword == NssKeywords.Const;
            if (!constness) --baseIndex;

            // Typename
            Type type = ConstructType(ref baseIndex);
            if (type == null) return null;

            // Identifier
            Lvalue lvalue = ConstructLvalue(ref baseIndex);
            if (lvalue == null) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0) return null;

            LvalueDecl ret = null;

            // Declaration
            if (token.GetType() == typeof(NssSeparator))
            {
                NssSeparator sep = (NssSeparator)token;
                if (sep.m_Separator != NssSeparators.Semicolon) return null;
                if (constness) return null;

                ret = new LvalueDecl();
                ret.m_Type = type;
                ret.m_Lvalue = lvalue;
            }
            // Declaration with assignment
            else if (token.GetType() == typeof(NssOperator))
            {
                NssOperator op = (NssOperator)token;
                if (op.m_Operator != NssOperators.Equals) return null;

                Value assign;
                assign = ConstructRvalue(ref baseIndex);
                if (assign == null)
                {
                    assign = ConstructLvalue(ref baseIndex);
                    if (assign == null) return null;
                }

                err = TraverseNextToken(out token, ref baseIndex);
                if (err != 0) return null;
                if (token.GetType() != typeof(NssSeparator) || ((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

                LvalueDeclWithAssignment decl = constness ? new ConstLvalueDeclWithAssignment() : new LvalueDeclWithAssignment();
                decl.m_Type = type;
                decl.m_Lvalue = lvalue;
                decl.m_AssignedValue = assign;
                ret = decl;
            }

            baseIndexRef = baseIndex;
            return ret;
        }

        public ArithmeticExpression ConstructArithmeticExpression(ref int baseIndexRef)
        {

            ArithmeticExpression ret = null;
            return ret;
        }

        public LogicalExpression ConstructLogicalExpression(ref int baseIndexRef)
        {

            LogicalExpression ret = null;
            return ret;
        }

        private FunctionCall ConstructFunctionCall(ref int baseIndexRef)
        {

            FunctionCall ret = null;
            return ret;
        }

        private LValueAssignment ConstructLvalueAssignment(ref int baseIndexRef)
        {

            LValueAssignment ret = null;
            return ret;
        }

        private WhileLoop ConstructWhileLoop(ref int baseIndexRef)
        {

            WhileLoop ret = null;
            return ret;
        }

        private ForLoop ConstructForLoop(ref int baseIndexRef)
        {

            ForLoop ret = null;
            return ret;
        }

        private DoWhileLoop ConstructDoWhileLoop(ref int baseIndexRef)
        {

            DoWhileLoop ret = null;
            return ret;
        }

        private IfStatement ConstructIfStatement(ref int baseIndexRef)
        {

            IfStatement ret = null;
            return ret;
        }

        private Block ConstructBlock_r(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.OpenCurlyBrace) return null;

            Block ret = new Block();

            while (true)
            {
                Block block = ConstructBlock_r(ref baseIndex);
                if (block != null)
                {
                    ret.m_Nodes.Add(block);
                    continue;
                }

                Node validInBlock = ConstructValidInBlock(ref baseIndex);
                if (validInBlock != null)
                {
                    ret.m_Nodes.Add(validInBlock);
                    continue;
                }

                err = TraverseNextToken(out token, ref baseIndex);
                if (err != 0) return null;
                if (token.GetType() == typeof(NssSeparator) && ((NssSeparator)token).m_Separator == NssSeparators.CloseCurlyBrace) break;

                // This is where we return null because unrecognised token, but for now we just break.
            }

            baseIndexRef = baseIndex;
            return ret;
        }

        private Node ConstructValidInBlock(ref int baseIndexRef)
        {
            { // FUNCTION CALL
                Node node = ConstructFunctionCall(ref baseIndexRef);
                if (node != null) return node;
            }

            { // VARIABLE DECLARATIONS
                Node node = ConstructLvalueDecl(ref baseIndexRef);
                if (node != null) return node;
            }

            { // VARIABLE ASSIGNMENTS
                Node node = ConstructLvalueAssignment(ref baseIndexRef);
                if (node != null) return node;
            }

            { // WHILE LOOP
                Node node = ConstructWhileLoop(ref baseIndexRef);
                if (node != null) return node;
            }

            { // FOR LOOP
                Node node = ConstructForLoop(ref baseIndexRef);
                if (node != null) return node;
            }

            { // DO WHILE LOOP
                Node node = ConstructDoWhileLoop(ref baseIndexRef);
                if (node != null) return node;
            }

            { // IF STATEMENT
                Node node = ConstructIfStatement(ref baseIndexRef);
                if (node != null) return node;
            }

            return null;
        }

        private Literal ConstructLiteral(ref int baseIndexRef)
        {
            Literal ret = null;
            return ret;
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
                Errors.Add(CompilationUnit.m_DebugInfo.m_SourceData[debugInfo.LineStart]);
                Errors.Add(string.Format(
                    "{0," + debugInfo.ColumnStart + "}" +
                    "{1," + (debugInfo.ColumnEnd - debugInfo.ColumnStart) + "}",
                    "^", "^"));
            }
        }

        private int TraverseNextToken(out NssToken token, ref int baseIndexRef, 
            bool skipComments = true, bool skipWhitespace = true)
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

                bool skip = false;

                if (skipWhitespace)
                {
                    if (ret is NssSeparator sep && (
                        sep.m_Separator == NssSeparators.Tab ||
                        sep.m_Separator == NssSeparators.Space ||
                        sep.m_Separator == NssSeparators.NewLine))
                    {
                        skip = true;
                    }
                }

                if (skipComments && ret.GetType() == typeof(NssComment))
                {
                    skip = true;
                }

                if (skip)
                {
                    ret = null;
                    ++baseIndex;
                    continue;
                }
            }

            baseIndexRef = ++baseIndex;
            token = ret;
            return 0;
        }
    }
}
