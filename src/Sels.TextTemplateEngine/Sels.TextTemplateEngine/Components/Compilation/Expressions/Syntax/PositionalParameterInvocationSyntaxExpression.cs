using Sels.Core;
using Sels.Core.Extensions.Conversion;
using Sels.Core.Extensions.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine.Compilation.Expressions.Syntax
{
    /// <summary>
    /// Invocation that is called with positional parameters.
    /// </summary>
    public class PositionalParameterInvocationSyntaxExpression : IParameterInvocationExpression<IPositionalParameterTextTemplateSyntaxExpression>
    {
        // Fields
        private readonly ITextTemplateToken[] _tokens;

        // Properties
        /// <inheritdoc/>
        public IReadOnlyList<IPositionalParameterTextTemplateSyntaxExpression> Parameters { get; }
        /// <inheritdoc/>
        public IIdentifierTextTemplateSyntaxExpression Identifier { get; }
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.PositionalInvocation;
        /// <inheritdoc/>
        public IEnumerable<ITextTemplateToken> Tokens
        {
            get
            {
                foreach (var token in _tokens)
                {
                    yield return token;
                }

                if (Body != null)
                {
                    foreach (var token in Body.Tokens)
                    {
                        yield return token;
                    }
                }
            }
        }
        /// <inheritdoc/>
        public ITextTemplateSyntaxExpression? Parent { get; set; }
        /// <inheritdoc/>
        public IReadOnlyList<ITextTemplateSyntaxExpression>? Children => Helper.Collection.EnumerateAll<ITextTemplateSyntaxExpression>(Identifier.AsEnumerable(), Parameters, Body.AsEnumerable()!).Where(x => x != null).ToList();
        /// <inheritdoc/>
        public ITemplateBodySyntaxExpression? Body { get; set; }

        /// <inheritdoc cref="PositionalParameterInvocationSyntaxExpression"/>
        /// <param name="identifier"><inheritdoc cref="Identifier"/></param>
        /// <param name="parameters"><inheritdoc cref="Parameters"/></param>
        /// <param name="tokens"><inheritdoc cref="Tokens"/></param>
        public PositionalParameterInvocationSyntaxExpression(IIdentifierTextTemplateSyntaxExpression identifier, IReadOnlyList<IPositionalParameterTextTemplateSyntaxExpression> parameters, IEnumerable<ITextTemplateToken> tokens)
        {
            Identifier = Guard.IsNotNull(identifier);
            Parameters = Guard.IsNotNull(parameters);
            Parameters.Execute(x => x.Parent = this);
            // Validate that we don't have duplicate indexes and no gaps in the indexes.
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters.Count(x => x.Index == i) != 1)
                {
                    throw new ArgumentException($"Duplicate or missing index {i} in positional parameters.");
                }
            }

            _tokens = Guard.IsNotNullOrEmpty(tokens).ToArray();
        }
    }
}
