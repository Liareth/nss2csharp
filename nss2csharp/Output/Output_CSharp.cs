using nss2csharp.Language;
using nss2csharp.Parser;
using System.Collections.Generic;

namespace nss2csharp.Output
{
    class Output_CSharp : IOutput
    {
        public int GetFromTokens(IEnumerable<IToken> tokens, out string data)
        {
            data = null;
            return 1;
        }

        public int GetFromCU(CompilationUnit cu, out string data)
        {
            if (cu.m_Metadata.m_Name == "nwscript.nss")
            {
                Output_CSharp_NwscriptNss output = new Output_CSharp_NwscriptNss();
                return output.GetFromCU(cu, out data);
            }
            else if (cu.m_Metadata.m_Name.StartsWith("NWNX_") && cu.m_Metadata.m_Name.EndsWith(".nss"))
            {
                Output_CSharp_NWNX output = new Output_CSharp_NWNX();
                return output.GetFromCU(cu, out data);
            }

            data = null;
            return 1;
        }

        public static string GetTypeAsString(Type type)
        {
            if (type.GetType() == typeof(VoidType))              return "void";
            else if (type.GetType() == typeof(IntType))          return "int";
            else if (type.GetType() == typeof(FloatType))        return "float";
            else if (type.GetType() == typeof(StringType))       return "string";
            else if (type.GetType() == typeof(StructType))       return ((StructType)type).m_TypeName;
            else if (type.GetType() == typeof(ObjectType))       return "NWN.Object";
            else if (type.GetType() == typeof(LocationType))     return "NWN.Location";
            else if (type.GetType() == typeof(VectorType))       return "NWN.Vector";
            else if (type.GetType() == typeof(ItemPropertyType)) return "NWN.ItemProperty";
            else if (type.GetType() == typeof(EffectType))       return "NWN.Effect";
            else if (type.GetType() == typeof(TalentType))       return "NWN.Talent";
            else if (type.GetType() == typeof(EventType))        return "NWN.Event";

            return null;
        }
    }
}
