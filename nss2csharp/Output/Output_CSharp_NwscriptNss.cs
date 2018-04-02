﻿using nss2csharp.Parser;
using System.Collections.Generic;
using System.Linq;

namespace nss2csharp.Output
{
    class Output_CSharp_NwscriptNss
    {
        public int GetFromCU(CompilationUnit cu, out string data)
        {
            List<string> lines = new List<string>();

            lines.Add("namespace NWN");
            lines.Add("{");
            lines.Add("    class NWScript");
            lines.Add("    {");

            foreach (Node node in cu.m_Nodes)
            {
                if (node is LvalueDeclWithAssignment lvalueDecl)
                {
                    string type = Output_CSharp.GetTypeAsString(lvalueDecl.m_Type);
                    string name = lvalueDecl.m_Lvalue.m_Identifier;
                    string value = lvalueDecl.m_Expression.m_Expression;
                    lines.Add(string.Format("        const {0} {1} = {2};", type, name, value));
                }

                if (node is FunctionDeclaration funcDecl)
                {
                    string name = funcDecl.m_Name.m_Identifier;
                    string retType = Output_CSharp.GetTypeAsString(funcDecl.m_ReturnType);

                    List<string> funcParams = new List<string>();
                    foreach (FunctionParameter param in funcDecl.m_Parameters)
                    {
                        string paramType = Output_CSharp.GetTypeAsString(param.m_Type);
                        string paramName = param.m_Lvalue.m_Identifier;

                        // TODO: Support defaults

                        funcParams.Add(paramType + " " + paramName);
                    }

                    string parameters = funcParams.Count == 0 ? "" : funcParams.Aggregate((a, b) => a + ", " + b);

                    lines.Add(string.Format("        public {0} {1} ({2});", retType, name, parameters));
                }
            }

            lines.Add("    }");
            lines.Add("}");

            data = lines.Aggregate((a, b) => a + "\n" + b);
            return 0;
        }
    }
}