using Sels.Core;
using Sels.Core.Extensions;
using Sels.Core.Extensions.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <inheritdoc cref="IPositionalParameterTextTemplateSyntaxExpression"/>
    public class NamedParameterSyntaxExpression : INamedParameterTextTemplateSyntaxExpression
    {
        // Fields
        private readonly ITextTemplateToken[] _tokens;

        // Properties
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression Argument { get; }
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.NamedParameter;
        /// <inheritdoc/>
        public IEnumerable<ITextTemplateToken> Tokens => _tokens;
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        public IReadOnlyList<ITextTemplateSyntaxExpression>? Children => Helper.Collection.Enumerate(Name, Argument).ToList();
        /// <inheritdoc/>
        public IIdentifierTextTemplateSyntaxExpression Name { get; }

        /// <inheritdoc cref="NamedParameterSyntaxExpression"/>
        /// <param name="argument"><inheritdoc cref="Argument"/></param>
        /// <param name="tokens">The tokens that were used to parse the current expression</param>
        public NamedParameterSyntaxExpression(IIdentifierTextTemplateSyntaxExpression name, ITextTemplateSyntaxExpression argument, IEnumerable<ITextTemplateToken> tokens)
        {
            Name = Guard.IsNotNull(name);
            Name.Parent = this;
            Argument = Guard.IsNotNull(argument);
            Argument.Parent = this;

            _tokens = Guard.IsNotNullOrEmpty(tokens).ToArray();
        }
    }
}
