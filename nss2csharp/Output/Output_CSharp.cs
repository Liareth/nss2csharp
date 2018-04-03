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

        public static string GetValueAsString(Value value)
        {
            const string floatFormatStr = "0.0#######";

            if (value is Lvalue lv)
            {
                return lv.m_Identifier;
            }
            else if (value is IntLiteral intLit)
            {
                return intLit.m_Value.ToString();
            }
            else if (value is FloatLiteral floatLit)
            {
                return floatLit.m_Value.ToString(floatFormatStr) + "f";
            }
            else if (value is StringLiteral stringLit)
            {
                return stringLit.m_Value;
            }
            else if (value is VectorLiteral vectorLiteral)
            {
                return string.Format("new NWN.Vector({0}, {1}, {2})",
                    vectorLiteral.m_X.m_Value.ToString(floatFormatStr) + "f",
                    vectorLiteral.m_Y.m_Value.ToString(floatFormatStr) + "f",
                    vectorLiteral.m_Z.m_Value.ToString(floatFormatStr) + "f");
            }
            else if (value is ObjectInvalidLiteral)
            {
                return "null";
            }
            else if (value is ObjectSelfLiteral)
            {
                return "null";
            }

            return null;
        }

        public static string GetStackPush(Type type, Value val)
        {
            if (type.GetType() == typeof(IntType)) return string.Format("NWN.Internal.StackPushInteger({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(FloatType)) return string.Format("NWN.Internal.StackPushFloat({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(StringType)) return string.Format("NWN.Internal.StackPushString({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(ObjectType)) return string.Format("NWN.Internal.StackPushObject({0}, {1})", GetValueAsString(val), val.GetType() == typeof(ObjectSelfLiteral) ? "true" : "false");
            else if (type.GetType() == typeof(LocationType)) return string.Format("NWN.Internal.StackPushLocation({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(VectorType)) return string.Format("NWN.Internal.StackPushVector({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(ItemPropertyType)) return string.Format("NWN.Internal.StackPushItemProperty({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(EffectType)) return string.Format("NWN.Internal.StackPushEffect({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(TalentType)) return string.Format("NWN.Internal.StackPushTalent({0})", GetValueAsString(val));
            else if (type.GetType() == typeof(EventType)) return string.Format("NWN.Internal.StackPushEvent({0})", GetValueAsString(val));

            return null;
        }

        public static string GetStackPop(Type type)
        {
            if (type.GetType() == typeof(IntType)) return "NWN.Internal.StackPopInteger()";
            else if (type.GetType() == typeof(FloatType)) return "NWN.Internal.StackPopFloat()";
            else if (type.GetType() == typeof(StringType)) return "NWN.Internal.StackPopString()";
            else if (type.GetType() == typeof(ObjectType)) return "NWN.Internal.StackPopObject()";
            else if (type.GetType() == typeof(LocationType)) return "NWN.Internal.StackPopLocation()";
            else if (type.GetType() == typeof(VectorType)) return "NWN.Internal.StackPopVector()";
            else if (type.GetType() == typeof(ItemPropertyType)) return "NWN.Internal.StackPopItemProperty()";
            else if (type.GetType() == typeof(EffectType)) return "NWN.Internal.StackPopEffect()";
            else if (type.GetType() == typeof(TalentType)) return "NWN.Internal.StackPopTalent()";
            else if (type.GetType() == typeof(EventType)) return "NWN.Internal.StackPopEvent()";

            return null;
        }

        public static string GetInternalCall(int id)
        {
            return string.Format("NWN.Internal.CallBuiltIn({0})", id);
        }
    }
}
