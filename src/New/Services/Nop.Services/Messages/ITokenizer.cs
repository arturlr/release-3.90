namespace Nop.Services.Messages;

public interface ITokenizer
{
    string Replace(string template, IEnumerable<Token> tokens, bool htmlEncode);
}
