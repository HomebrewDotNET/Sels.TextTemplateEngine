using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation
{
    /// <summary>
    /// Lexer that reads a text stream and converts it into tokens for a <see cref="ITextTemplateParser"/> to parse.
    /// </summary>
    public interface ITextTemplateLexer
    {
        /// <summary>
        /// Reads <paramref name="stream"/> and enumerates the tokens found in the stream.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="encoding">The encoding of <paramref name="stream"/> if known</param>
        /// <param name="ownsStream">If the lexers owns the stream and can dispose it when done, set to false if caller does the disposing</param>
        /// <param name="bufferLength">How many characters will be read at a time from <paramref name="stream"/></param>
        /// <param name="tokenLexers">The lexers to use to lex in addition to the default configured ones</param>
        /// <param name="cancellationToken">Optional token to cancel the request</param>
        /// <returns>Async enumerator that will return any tokens reads from <paramref name="stream"/></returns>
        public IAsyncEnumerable<ITextTemplateToken> LexAsync(IEnumerable<ITextTemplateTokenLexer> tokenLexers, Stream stream, Encoding? encoding = null, bool ownsStream = true, int bufferLength = 1024, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Context that contains the current state of the lexer when reading a stream.
    /// </summary>
    public interface ITextTemplateLexerContext
    {
        /// <summary>
        /// The stream being read.
        /// </summary>
        public Stream Source { get; }
        /// <summary>
        /// The current position of <see cref="CurrentCharacter"/>.
        /// </summary>
        public int Position { get; }
        /// <summary>
        /// The position of the first character in <see cref="Buffer"/>.
        /// </summary>
        public int BufferPosition => Position - Buffer.Count+1;
        /// <summary>
        /// The current line in the stream.
        /// </summary>
        public int Line { get; }
        /// <summary>
        /// The current character buffer that hasn't been lexed yet.
        /// </summary>
        public IReadOnlyList<char> Buffer { get; }
        /// <summary>
        /// The current character being read.
        /// </summary>
        public char CurrentCharacter => Buffer.Last();
        /// <summary>
        /// The character read before <see cref="CurrentCharacter"/>. Can be null if the buffer only contains <see cref="CurrentCharacter"/>.
        /// </summary>
        public char? PreviousCharacter => Buffer.Skip(Buffer.Count-2).FirstOrDefault();
        /// <summary>
        /// Indicates if <see cref="CurrentCharacter"/> is the last character in the stream.
        /// </summary>
        public bool IsLastCharacter { get; }
        /// <summary>
        /// All the token lexers that are available to lex tokens in the current context.
        /// </summary>
        public IReadOnlyList<ITextTemplateTokenLexer> TokenLexers { get; }
    }
}
