﻿using nss2csharp.Language;
using nss2csharp.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace nss2csharp.Output
{
    class Output_Nss : IOutput
    {
        public int GetFromTokens(IEnumerable<IToken> tokens, out string data)
        {
            StringBuilder builder = new StringBuilder();
            Language_Nss nss = new Language_Nss();

            foreach (IToken token in tokens)
            {
                string tokenAsStr = nss.StringFromToken(token);

                if (tokenAsStr == null)
                {
                    data = null;
                    return 1;
                }

                builder.Append(tokenAsStr);
            }

            data = builder.ToString();
            return 0;
        }

        public int GetFromCU(CompilationUnit cu, out string data)
        {
            data = null;
            return 1;
        }
    }
}
