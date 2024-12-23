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
    /// Invocation that is called with named parameters.
    /// </summary>
    public class NamedParameterInvocationSyntaxExpression : IParameterInvocationExpression<INamedParameterTextTemplateSyntaxExpression>
    {
        // Fields
        private readonly ITextTemplateToken[] _tokens;

        // Properties
        /// <inheritdoc/>
        public IReadOnlyList<INamedParameterTextTemplateSyntaxExpression> Parameters { get; }
        /// <inheritdoc/>
        public IIdentifierTextTemplateSyntaxExpression Identifier { get; }
        /// <inheritdoc/>
        public string Type => TextTemplateEngineConstants.Compilation.ExpressionTypes.NamedInvocation;
        /// <inheritdoc/>
        public IEnumerable<ITextTemplateToken> Tokens { 
            get {
                foreach (var token in _tokens)
                {
                    yield return token;
                }

                if(Body != null)
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

        /// <inheritdoc cref="NamedParameterInvocationSyntaxExpression"/>
        /// <param name="identifier"><inheritdoc cref="Identifier"/></param>
        /// <param name="parameters"><inheritdoc cref="Parameters"/></param>
        /// <param name="tokens"><inheritdoc cref="Tokens"/></param>
        public NamedParameterInvocationSyntaxExpression(IIdentifierTextTemplateSyntaxExpression identifier, IReadOnlyList<INamedParameterTextTemplateSyntaxExpression> parameters, IEnumerable<ITextTemplateToken> tokens)
        {
            Identifier = Guard.IsNotNull(identifier);
            Identifier.Parent = this;
            Parameters = Guard.IsNotNull(parameters);
            Parameters.Execute(x => x.Parent = this);
            // Validate that we don't have duplicate names
            var grouped = Parameters.GroupAsDictionary(x => new string(x.Name.Identifier.ToArray()), StringComparer.OrdinalIgnoreCase);
            grouped.Where(x => x.Value.Count > 1).Execute(x => throw new ArgumentException($"Parameter <{x.Key}> is defined <{x.Value.Count}> times"));

            _tokens = Guard.IsNotNullOrEmpty(tokens).ToArray();
        }
    }
}
