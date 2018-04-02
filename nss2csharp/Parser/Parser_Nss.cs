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
            // - Struct declarations

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

            { // STRUCT DECLARATIONS
                Node node = ConstructStructDeclaration(ref baseIndexRef);
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
                    param.m_Lvalue = paramName;
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
                        param.m_Lvalue = paramName;
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

        private LvaluePreinc ConstructLvaluePreinc(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Addition) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Addition) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssIdentifier)) return null;
            string identifier = ((NssIdentifier)token).m_Identifier;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

            LvaluePreinc ret = new LvaluePreinc { m_Identifier = identifier };
            baseIndexRef = baseIndex;
            return ret;
        }

        private LvaluePostinc ConstructLValuePostinc(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssIdentifier)) return null;
            string identifier = ((NssIdentifier)token).m_Identifier;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Addition) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Addition) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

            LvaluePostinc ret = new LvaluePostinc { m_Identifier = identifier };
            baseIndexRef = baseIndex;
            return ret;
        }

        private LvaluePredec ConstructLvaluePredec(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Subtraction) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Subtraction) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssIdentifier)) return null;
            string identifier = ((NssIdentifier)token).m_Identifier;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

            LvaluePredec ret = new LvaluePredec { m_Identifier = identifier };
            baseIndexRef = baseIndex;
            return ret;
        }

        private LvaluePostdec ConstructLValuePostdec(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssIdentifier)) return null;
            string identifier = ((NssIdentifier)token).m_Identifier;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Subtraction) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssOperator)) return null;
            if (((NssOperator)token).m_Operator != NssOperators.Subtraction) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

            LvaluePostdec ret = new LvaluePostdec { m_Identifier = identifier };
            baseIndexRef = baseIndex;
            return ret;
        }

        private Lvalue ConstructLvalue(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssIdentifier)) return null;
            string identifier = ((NssIdentifier)token).m_Identifier;

            Lvalue ret = new Lvalue { m_Identifier = identifier };
            baseIndexRef = baseIndex;
            return ret;
        }

        private Rvalue ConstructRvalue(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0) return null;

            bool negative = false;

            if (token.GetType() == typeof(NssOperator))
            {
                if (((NssOperator)token).m_Operator != NssOperators.Subtraction) return null;
                err = TraverseNextToken(out token, ref baseIndex);
                if (err != 0) return null;
                negative = true;
            }

            if (token.GetType() != typeof(NssLiteral) )return null;

            Rvalue ret = null;

            NssLiteral lit = (NssLiteral)token;
            string literal = (negative ? "-" : "") + lit.m_Literal;

            switch (lit.m_LiteralType)
            {
                case NssLiteralType.Int:
                {
                    if (!int.TryParse(literal, out int value)) return null;
                    ret = new IntLiteral { m_Value = value };
                    break;
                }

                case NssLiteralType.Float:
                {
                    if (!float.TryParse(literal, out float value)) return null;
                    ret = new FloatLiteral { m_Value = value };
                    break;
                }

                case NssLiteralType.String:
                    ret = new StringLiteral { m_Value = literal };
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

                ArithmeticExpression expr = ConstructArithmeticExpression(ref baseIndex);
                if (expr == null) return null;

                LvalueDeclWithAssignment decl = constness ? new ConstLvalueDeclWithAssignment() : new LvalueDeclWithAssignment();
                decl.m_Type = type;
                decl.m_Lvalue = lvalue;
                decl.m_Expression = expr;
                ret = decl;
            }

            baseIndexRef = baseIndex;
            return ret;
        }

        private StructDeclaration ConstructStructDeclaration(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.Struct) return null;

            Lvalue structName = ConstructLvalue(ref baseIndex);
            if (structName == null) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.OpenCurlyBrace) return null;

            StructDeclaration ret = new StructDeclaration
            {
                m_Name = structName,
                m_Members = new List<LvalueDecl>()
            };

            while (true)
            {
                LvalueDecl decl = ConstructLvalueDecl(ref baseIndex);
                if (decl == null)
                {
                    err = TraverseNextToken(out token, ref baseIndex);
                    if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
                    if (((NssSeparator)token).m_Separator != NssSeparators.CloseCurlyBrace) return null;
                    break;
                }

                ret.m_Members.Add(decl);
            }

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

            baseIndexRef = baseIndex;
            return ret;
        }

        public AssignmentOpChain ConstructAssignmentOpChain(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            NssOperators?[] ops = new NssOperators?[] { null, null };

            for (int i = 0; i < ops.Length; ++i)
            {
                int err = TraverseNextToken(out NssToken token, ref baseIndex);
                if (err != 0) return null;

                NssOperator op = token as NssOperator;
                if (op == null)
                {
                    --baseIndex; // Step back for caller.
                    break;
                }

                ops[i] = op.m_Operator;
            }

            if (!ops[0].HasValue) return null;

            AssignmentOpChain ret = null;

            if (ops[0].Value == NssOperators.Equals)
            {
                ret = new Equals();
            }
            else if (ops[0].Value == NssOperators.Addition)
            {
                if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
                {
                    ret = new PlusEquals();
                }
            }
            else if (ops[0].Value == NssOperators.Subtraction)
            {
                if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
                {
                    ret = new PlusEquals();
                }
            }
            else if (ops[0].Value == NssOperators.Multiplication)
            {
                if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
                {
                    ret = new MultiplyEquals();
                }
            }
            else if (ops[0].Value == NssOperators.Division)
            {
                if (ops[1].HasValue && ops[1].Value == NssOperators.Equals)
                {
                    ret = new DivideEquals();
                }
            }

            if (ret == null) return null;

            baseIndexRef = baseIndex;
            return ret;
        }

        public ArithmeticExpression ConstructArithmeticExpression(ref int baseIndexRef, NssSeparators boundingSep = NssSeparators.Semicolon)
        {
            int baseIndex = baseIndexRef;
            string expression = "";

            while (true)
            {
                int err = TraverseNextToken(out NssToken token, ref baseIndex);
                if (err != 0) return null;
                if (token.GetType() == typeof(NssSeparator) && ((NssSeparator)token).m_Separator == boundingSep) break;

                expression += token.ToString();

                if (token.GetType() == typeof(NssKeyword) || token.GetType() == typeof(NssIdentifier))
                {
                    expression += " ";
                }
            }

            ArithmeticExpression ret = new ArithmeticExpression { m_Expression = expression.TrimEnd() };
            baseIndexRef = baseIndex;
            return ret;
        }

        public LogicalExpression ConstructLogicalExpression(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.OpenParen) return null;

            string expression = "";
            int parenDepth = 1;

            while (true)
            {
                err = TraverseNextToken(out token, ref baseIndex);
                if (err != 0) return null;

                if (token is NssSeparator sep)
                {
                    if (sep.m_Separator == NssSeparators.OpenParen)
                    {
                        ++parenDepth;
                    }
                    else if (sep.m_Separator == NssSeparators.CloseParen)
                    {
                        --parenDepth;
                    }
                    else if (sep.m_Separator != NssSeparators.Comma)
                    {
                        return null;
                    }
                }

                if (parenDepth == 0)
                {
                    break;
                }

                expression += token.ToString();

                if (token.GetType() == typeof(NssKeyword) || token.GetType() == typeof(NssIdentifier))
                {
                    expression += " ";
                }
            }

            if (parenDepth != 0) return null;

            LogicalExpression ret = new LogicalExpression { m_Expression = expression.TrimEnd() };
            baseIndexRef = baseIndex;
            return ret;
        }

        private FunctionCall ConstructFunctionCall(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            // For now, we're gonna treat a function call like an lvalue + a logical expression.
            // This obviously isn't true but we really don't care about the semantics for this tool.

            Lvalue functionName = ConstructLvalue(ref baseIndex);
            if (functionName == null) return null;

            LogicalExpression args = ConstructLogicalExpression(ref baseIndex);
            if (args == null) return null;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

            FunctionCall ret = new FunctionCall { m_Name = functionName, m_Arguments = args };
            baseIndexRef = baseIndex;
            return ret;
        }

        private LvalueAssignment ConstructLvalueAssignment(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            Lvalue lvalue = ConstructLvalue(ref baseIndex);
            if (lvalue == null) return null;

            AssignmentOpChain opChain = ConstructAssignmentOpChain(ref baseIndex);
            if (opChain == null) return null;

            ArithmeticExpression expr = ConstructArithmeticExpression(ref baseIndex);
            if (expr == null) return null;

            LvalueAssignment ret = new LvalueAssignment { m_Lvalue = lvalue, m_OpChain = opChain, m_Expression = expr };
            baseIndexRef = baseIndex;
            return ret;
        }

        private WhileLoop ConstructWhileLoop(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.While) return null;

            LogicalExpression cond = ConstructLogicalExpression(ref baseIndex);
            if (cond == null) return null;

            Node action = ConstructBlock_r(ref baseIndex);
            if (action == null)
            {
                action = ConstructValidInBlock(ref baseIndex);
                if (action == null) return null;
            }

            WhileLoop ret = new WhileLoop { m_Expression = cond, m_Action = action };
            baseIndexRef = baseIndex;
            return ret;
        }

        private ForLoop ConstructForLoop(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.For) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.OpenParen) return null;

            ArithmeticExpression pre = ConstructArithmeticExpression(ref baseIndex);
            if (pre == null) return null;

            ArithmeticExpression cond = ConstructArithmeticExpression(ref baseIndex);
            if (cond == null) return null;

            ArithmeticExpression post = ConstructArithmeticExpression(ref baseIndex, NssSeparators.CloseParen);
            if (post == null) return null;

            Node action = ConstructBlock_r(ref baseIndex);
            if (action == null)
            {
                action = ConstructValidInBlock(ref baseIndex);
                if (action == null) return null;
            }

            ForLoop ret = new ForLoop
            {
                m_Pre = pre,
                m_Condition = cond,
                m_Post = post,
                m_Action = action
            };

            baseIndexRef = baseIndex;
            return ret;
        }

        private DoWhileLoop ConstructDoWhileLoop(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.Do) return null;

            Node action = ConstructBlock_r(ref baseIndex);
            if (action == null)
            {
                action = ConstructValidInBlock(ref baseIndex);
                if (action == null) return null;
            }

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.While) return null;

            LogicalExpression cond = ConstructLogicalExpression(ref baseIndex);
            if (cond == null) return null;

            err = TraverseNextToken(out token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssSeparator)) return null;
            if (((NssSeparator)token).m_Separator != NssSeparators.Semicolon) return null;

            DoWhileLoop ret = new DoWhileLoop { m_Expression = cond, m_Action = action };
            baseIndexRef = baseIndex;
            return ret;
        }

        private IfStatement ConstructIfStatement(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.If) return null;

            LogicalExpression expression = ConstructLogicalExpression(ref baseIndex);
            if (expression == null) return null;

            Node action = ConstructBlock_r(ref baseIndex);
            if (action == null)
            {
                action = ConstructValidInBlock(ref baseIndex);
                if (action == null) return null;
            }

            IfStatement ret = new IfStatement { m_Expression = expression, m_Action = action };
            baseIndexRef = baseIndex;
            return ret;
        }

        private ElseStatement ConstructElseStatement(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.Else) return null;

            Node action = ConstructBlock_r(ref baseIndex);
            if (action == null)
            {
                action = ConstructValidInBlock(ref baseIndex);
                if (action == null) return null;
            }

            ElseStatement ret = new ElseStatement { m_Action = action };
            baseIndexRef = baseIndex;
            return ret;
        }

        private ReturnStatement ConstructReturnStatement(ref int baseIndexRef)
        {
            int baseIndex = baseIndexRef;

            int err = TraverseNextToken(out NssToken token, ref baseIndex);
            if (err != 0 || token.GetType() != typeof(NssKeyword)) return null;
            if (((NssKeyword)token).m_Keyword != NssKeywords.Return) return null;

            ArithmeticExpression expr = ConstructArithmeticExpression(ref baseIndex);
            if (expr == null) return null;

            ReturnStatement ret = new ReturnStatement { m_Expression = expr };
            baseIndexRef = baseIndex;
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

                ReportTokenError(token, "Unrecognised token in block-level.");

                return null;
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

            { // LVALUE PREINC
                Node node = ConstructLvaluePreinc(ref baseIndexRef);
                if (node != null) return node;
            }

            { // LVALUE POSTINC
                Node node = ConstructLValuePostinc(ref baseIndexRef);
                if (node != null) return node;
            }

            { // LVALUE PREDEC
                Node node = ConstructLvaluePredec(ref baseIndexRef);
                if (node != null) return node;
            }

            { // LVALUE POSTDEC
                Node node = ConstructLValuePostdec(ref baseIndexRef);
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

            { // ELSE STATEMENT
                Node node = ConstructElseStatement(ref baseIndexRef);
                if (node != null) return node;
            }

            { // RETURN STATEMENT
                Node node = ConstructReturnStatement(ref baseIndexRef);
                if (node != null) return node;
            }

            return null;
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
