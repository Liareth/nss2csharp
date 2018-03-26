using System;
using System.Collections.Generic;

namespace nss2csharp
{
    class Output_Nss : IOutput
    {
        public int GetFromTokens(IEnumerable<IToken> tokens, out string data)
        {
            throw new NotImplementedException();
        }

        public int GetFromCU(NssCompilationUnit cu, out string data)
        {
            throw new NotImplementedException();
        }
    }
}
