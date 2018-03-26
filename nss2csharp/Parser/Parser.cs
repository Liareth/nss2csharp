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
    }

    public class NssCompilationUnit : NssNode
    {
        public string m_Name; // Script name
        public List<NssNode> m_Children = new List<NssNode>(); // Constants, declarations, implementations - in order of original source file.
    }

    public class NssFunctionDeclaration : NssNode
    {
        public string m_Name;
        public List<NssLvalue> m_Parameters;
    }

    public class NssFunctionImplementation : NssNode
    {
        public string m_Name;
        public List<NssLvalue> m_Parameters;
        public List<NssNode> m_Children = new List<NssNode>(); // The function body - in order of original source file.
    }

    public class NssConstant : NssNode
    {
        public NssType m_Type; // A constant can only be an int, string, or float.
        public string m_Name; // The actual name of this constant.
        public string m_Value; // It's safe for us to store the value as a string as we never really need to process it.
    }

    public class NssLvalue : NssNode
    {
        public NssType m_Type;
        public string m_Name;
    }

    public class NssFunctinParameter : NssLvalue
    {
        public string m_Defaultvalue; // Contains the default value in string format - if there is one. Else, null.
    }

    public class NssStruct : NssNode
    {
    }

    public class NssParser
    {
        public int Parse(string name, List<NssToken> tokens, out NssCompilationUnit cu)
        {
            cu = new NssCompilationUnit();
            cu.m_Name = name;

            return 1;
        }
    }
}
