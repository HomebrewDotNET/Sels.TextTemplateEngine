using Sels.Core;
using Sels.Core.Extensions.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <inheritdoc cref="IPositionalParameterTextTemplateSyntaxExpression"/>
    public class PositionalParameterSyntaxExpression : IPositionalParameterTextTemplateSyntaxExpression
    {
        // Fields
        private readonly ITextTemplateToken[] _tokens;

        // Properties
        /// <inheritdoc/>
        public required int Index { get; init; }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression Argument { get; }
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.PositionalParameter;
        /// <inheritdoc/>
        public IEnumerable<ITextTemplateToken> Tokens => _tokens;
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        public IReadOnlyList<ITextTemplateSyntaxExpression>? Children => Argument.AsArray();

        /// <inheritdoc cref="PositionalParameterSyntaxExpression"/>
        /// <param name="argument"><inheritdoc cref="Argument"/></param>
        /// <param name="tokens">The tokens that were used to parse the current expression</param>
        public PositionalParameterSyntaxExpression(ITextTemplateSyntaxExpression argument, IEnumerable<ITextTemplateToken> tokens)
        {
            Argument = Guard.IsNotNull(argument);
            Argument.Parent = this;

            _tokens = Guard.IsNotNullOrEmpty(tokens).ToArray();
        }
    }
}
