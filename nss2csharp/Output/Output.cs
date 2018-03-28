using nss2csharp.Language;
using nss2csharp.Parser;
using System.Collections.Generic;

namespace nss2csharp.Output
{
    public interface IOutput
    {
        int GetFromTokens(IEnumerable<IToken> tokens, out string data);

        int GetFromCU(CompilationUnit cu, out string data);
    }
}
