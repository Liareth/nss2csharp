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

                int err = Parse_r(CompilationUnit, ref baseIndex);
                if (err != 0)
                {
                    return err;
                }
            }

            return 0;
        }

        private int Parse_r(NssNode parent, ref int baseIndex)
        {
            NssToken token = Tokens[baseIndex];

            // Do stuff ...

            Errors.Add(string.Format("Unrecognised / unhandled token: [{0}]\n'{1}'", token.GetType().FullName, token.ToString()));
            return 1;
        }
    }
}
