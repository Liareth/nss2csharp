using nss2csharp.Parser;
using System.Collections.Generic;
using System.Linq;

namespace nss2csharp.Output
{
    class Output_CSharp_NwscriptNss
    {
        // This is a whitelist of functions we've implementedo ourselves.
        private static List<string> s_BuiltIns = new List<string>
        {
            // ACTION FUNCTIONS
            "AssignCommand",
            "DelayCommand",
            "ActionDoCommand"
        };

        public int GetFromCU(CompilationUnit cu, out string data)
        {
            List<string> lines = new List<string>();

            lines.Add("namespace NWN");
            lines.Add("{");
            lines.Add("    class NWScript");
            lines.Add("    {");

            int internalCallId = 0;

            foreach (Node node in cu.m_Nodes)
            {
                if (node is LineComment lineComment)
                {
                    lines.Add("        // " + lineComment.m_Comment);
                }

                if (node is BlockComment blockComment)
                {
                    lines.Add("        /*");
                    foreach (string line in blockComment.m_CommentLines)
                    {
                        lines.Add("        " + line);
                    }
                    lines.Add("        */");
                }

                if (node is LvalueDeclWithAssignment lvalueDecl)
                {
                    string type = Output_CSharp.GetTypeAsString(lvalueDecl.m_Type);
                    string name = lvalueDecl.m_Lvalue.m_Identifier;
                    string value = lvalueDecl.m_Expression.m_Expression;
                    lines.Add(string.Format("        public const {0} {1} = {2}{3};", type, name, value,
                        lvalueDecl.m_Type.GetType() == typeof(FloatType) && !value.EndsWith("f") ? "f" : ""));
                }

                if (node is FunctionDeclaration funcDecl)
                {
                    string name = funcDecl.m_Name.m_Identifier;

                    if (s_BuiltIns.Contains(name))
                    {
                        continue;
                    }

                    string retType = Output_CSharp.GetTypeAsString(funcDecl.m_ReturnType);

                    List<string> funcParams = new List<string>();
                    foreach (FunctionParameter param in funcDecl.m_Parameters)
                    {
                        string paramType = Output_CSharp.GetTypeAsString(param.m_Type);
                        string paramName = param.m_Lvalue.m_Identifier;
                        string paramStr = paramType + " " + paramName;

                        if (param is FunctionParameterWithDefault def)
                        {
                            string defaultAsStr = Output_CSharp.GetValueAsString(def.m_Default);

                            if (defaultAsStr == "OBJECT_TYPE_INVALID")
                            {
                                // HACK: This function does something that is clearly wrong:
                                // void SpeakOneLinerConversation(string sDialogResRef="", object oTokenTarget=OBJECT_TYPE_INVALID);
                                // I don't know how it actually compiles in nwscript.nss - I bet the compiler has a hack for it too.
                                // We'll just alias to OBJECT_INVALID in that case.
                                defaultAsStr = "OBJECT_INVALID";
                            }

                            paramStr += " = " + defaultAsStr;
                        }

                        funcParams.Add(paramStr);
                    }

                    string parameters = funcParams.Count == 0 ? "" : funcParams.Aggregate((a, b) => a + ", " + b);

                    lines.Add(string.Format("        public {0} {1}({2})", retType, name, parameters));
                    lines.Add("        {");

                    foreach (FunctionParameter param in funcDecl.m_Parameters)
                    {
                        lines.Add("            " + Output_CSharp.GetStackPush(param.m_Type, param.m_Lvalue) + ";");
                    }

                    lines.Add("            " + Output_CSharp.GetInternalCall(internalCallId++) + ";");

                    if (funcDecl.m_ReturnType.GetType() != typeof(VoidType))
                    {
                        lines.Add("            return " + Output_CSharp.GetStackPop(funcDecl.m_ReturnType) + ";");
                    }

                    lines.Add("        }");
                    lines.Add("");
                }
            }

            lines.Add("    }");
            lines.Add("}");

            data = lines.Aggregate((a, b) => a + "\n" + b);
            return 0;
        }
    }
}
