using Sels.Core.Extensions.Reflection;
using Sels.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sels.Core;
using Sels.TextTemplateEngine.Compilation.Expressions.Syntax;

namespace Sels.TextTemplateEngine.Expressions.Syntax
{
    /// <summary>
    /// Expression that represents the root of an expression tree.
    /// </summary>
    public class SyntaxTreeRootExpression : TemplateBodySyntaxExpression
    {
        /// <inheritdoc/>
        public override string Type => TextTemplateEngineConstants.Compilation.SyntaxExpressionTypes.Root;
    }
}
