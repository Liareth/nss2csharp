using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nss2csharp.Parser
{

    public struct CompilationUnitMetadata
    {
        public string m_Name;
    }

    public struct CompilationUnitDebugInfo
    {
        public string[] m_SourceData;
    }

    public class CompilationUnit : Node
    {
        public CompilationUnitMetadata m_Metadata;
        public CompilationUnitDebugInfo m_DebugInfo;
        public List<Node> m_Nodes = new List<Node>();
    }

    public class LvalueDecl : Node
    {
        public Type m_Type;
        public Lvalue m_Lvalue;
    }

    public class LvalueDeclWithAssignment : LvalueDecl
    {
        public Value m_AssignedValue;
    }

    public class ConstLvalueDeclWithAssignment : LvalueDeclWithAssignment
    { }

    public class FunctionParameter : Node
    {
        public Type m_Type;
        public Lvalue m_Name;
    }

    public class FunctionParameterWithDefault : FunctionParameter
    {
        public Rvalue m_Default;
    }

    public class Block : Node
    {
        public List<Node> m_Nodes = new List<Node>();
    }

    public class FunctionDeclaration : Function
    { }

    public class FunctionImplementation : Function
    {
        public Block m_Block;
    }
}
