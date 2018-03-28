namespace nss2csharp.Language
{
    public interface IToken
    {
        object UserData { get; set; }
    }

    public interface ILanguage
    {
        // Returns the string representation of the token.
        string StringFromToken(IToken token);
    }
}
