using System.Collections.Generic;

namespace nss2csharp.Parser
{
    public class Node
    { }

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

    public abstract class Type : Node
    { }

    public class VoidType : Type
    { }

    public class IntType : Type
    { }

    public class FloatType : Type
    { }

    public class StringType : Type
    { }

    public class StructType : Type
    {
        public string m_TypeName;
    }

    public class ObjectType : Type
    { }

    public class LocationType : Type
    { }

    public class VectorType : Type
    { }

    public class ItemPropertyType : Type
    { }

    public class EffectType : Type
    { }

    public class TalentType : Type
    { }

    public class ActionType : Type
    { }

    public abstract class Value : Node
    { }

    public class Lvalue : Value
    {
        public string m_Identifier;
    }

    public abstract class Rvalue : Value
    { }

    public abstract class Literal : Rvalue
    { }

    public class IntLiteral : Literal
    {
        public int m_Value;
    }

    public class FloatLiteral : Literal
    {
        public float m_Value;
    }

    public class StringLiteral : Literal
    {
        public string m_Value;
    }

    public abstract class Function : Node
    {
        public List<FunctionParameter> m_Parameters = new List<FunctionParameter>();
    }

    public abstract class AssignmentOpChain : Node
    { }

    public class Equals : AssignmentOpChain
    { }

    public class PlusEquals : AssignmentOpChain
    { }

    public class MinusEquals : AssignmentOpChain
    { }

    public class DivideEquals : AssignmentOpChain
    { }

    public class MultiplyEquals : AssignmentOpChain
    { }
}
