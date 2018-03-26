using System.Collections.Generic;

namespace nss2csharp
{
    public interface IOutput
    {
        int GetFromTokens(IEnumerable<IToken> tokens, out string data);

        int GetFromCU(NssCompilationUnit cu, out string data);
    }
}
