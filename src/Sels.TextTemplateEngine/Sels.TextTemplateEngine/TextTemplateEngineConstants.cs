using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine
{
    /// <summary>
    /// Contains constants related to the text template engine.
    /// </summary>
    public static class TextTemplateEngineConstants
    {
        /// <summary>
        /// Contains constants related to the compilation of text templates.
        /// </summary>
        public static class Compilation
        {
            /// <summary>
            /// Contains the known token types.
            /// </summary>
            public static class TokenTypes
            {
                /// <summary>
                /// Token contains a list of characters.
                /// </summary>
                public const string Text = "Text";
                /// <summary>
                /// Token that contains a list of characters that represent an (variable, method, ...) identifier.
                /// </summary>
                public const string Identifier = "Identifier";
                /// <summary>
                /// Token contains a whitespace character.
                /// </summary>
                public const string Whitespace = "Whitespace";
                /// <summary>
                /// Token contains a list of characters that represent a new line.
                /// </summary>
                public const string NewLine = "NewLine";
                /// <summary>
                /// Token used to start an expression tag.
                /// </summary>
                public const string StartExpression = "StartExpressionTag";
                /// <summary>
                /// Token used to start an expression that accesses the object returned from a variable or expression and optionally selects sub properties.
                /// </summary>
                public const string AccessorStart = "AccessorStart";
                /// <summary>
                /// Token used to end an expression tag.
                /// </summary>
                public const string EndExpression = "EndExpressionTag";
                /// <summary>
                /// Token that indicates that the current expression tag starts an expression with a body.
                /// </summary>
                public const string BlockStart = "BlockExpression";
                /// <summary>
                /// Token that indicates that the current expression tag ends an expression with a body.
                /// </summary>
                public const string BlockEnd = "BlockEndExpression";
                /// <summary>
                /// Token that indicates that the current expression is a comment.
                /// </summary>
                public const string Comment = "Comment";
                /// <summary>
                /// Token that indicates that the current expression contains a group of other expressions.
                /// </summary>
                public const string OperationGroup = "OperationGroup";
                /// <summary>
                /// Token that indicates that the sub property might be null and should return the default value of the type if it is.
                /// </summary>
                public const string ElvisOperator = "ElvisOperator";
                /// <summary>
                /// Token that indicates that the sub property should be accessed.
                /// </summary>
                public const string SubPropertyOrIdentifierDivisor = "SubPropertyOrIdentifierDivisor";
                /// <summary>
                /// Token that indicates that an invocation that uses named parameters is being called.
                /// </summary>
                public const string InvocationNamedParameterStart = "InvocationNamedParameterStart";
                /// <summary>
                /// Token that indicates that an invocation that uses positional parameters is being called or the start of an embedded expression group.
                /// </summary>
                public const string InvocationParameterOrGroupStart = "InvocationParameterOrGroupStart";
                /// <summary>
                /// Token that ends an invocation using positional parameters or the end of an embedded expression group.
                /// </summary>
                public const string InvocationParameterOrGroupEnd = "InvocationParameterOrGroupEnd";
                /// <summary>
                /// Token that indicates that a value is being assigned to an expression.
                /// </summary>
                public const string AssignmentOperator = "AssignmentOperator";
                /// <summary>
                /// Token that indicates the end of an expression that's being assigned to a positional invocation parameter.
                /// </summary>
                public const string PositionalParameterSplit = "PositionalParameterSplit";
                /// <summary>
                /// Token that indicates the end of an expression that's being assigned to a named invocation parameter.
                /// </summary>
                public const string NamedParameterSplit = "NamedParameterSplit";
            }

            /// <summary>
            /// Contains constants related to the syntax used in text templates.
            /// </summary>
            public static class Syntax
            {
                /// <summary>
                /// Token that indicates the start of a template expression tag.
                /// </summary>
                public const string StartToken = "${{";
                /// <summary>
                /// Token that indicates the start of an expression that accesses the object returned from a variable or expression and optionally selects sub properties.
                /// </summary>
                public const string AccessorStartToken = "@{{";
                /// <summary>
                /// Token that closes a template expression tag.
                /// </summary>
                public const string EndToken = "}}";
                /// <summary>
                /// Token that indicates that the current expression starts an expression with a body.
                /// </summary>
                public const string BlockStartToken = "#";
                /// <summary>
                /// Token that indicates that the current expression ends an expression with a body.
                /// </summary>
                public const string BlockEndToken = "/";
                /// <summary>
                /// Token that indicates the current expression is a comment.
                /// </summary>
                public const string CommentStartToken = "!";
                /// <summary>
                /// Token that indicates the current expression contains a group of expressions that produce a result based on operations in the group.
                /// </summary>
                public const string ExpressionOperationGroupToken = "*";
                /// <summary>
                /// Character that indicates that a sub property might be null and should return the default value of the type if it is.
                /// </summary>
                public const string ElvisOperator = "?";
                /// <summary>
                /// Character that indicates that a sub property should be accessed or could be a . character in a method call identifier.
                /// </summary>
                public const string SubPropertyOrIdentifierDivisor = ".";
                /// <summary>
                /// Character that indicates that an invocation that uses named parameters is being called.
                /// </summary>
                public const string InvocationNamedParameterStart = ":";
                /// <summary>
                /// Character that indicates that an invocation that uses positional parameters is being called or the start of an embedded expression group.
                /// </summary>
                public const string InvocationParameterOrGroupStart = "(";
                /// <summary>
                /// Character that ends an invocation using positional parameters or the end of an embedded expression group.
                /// </summary>
                public const string InvocationParameterOrGroupEnd = ")";
                /// <summary>
                /// Character that indicates that a value is being assigned to an expression.
                /// </summary>
                public const string AssignmentOperator = "=";
                /// <summary>
                /// Character that indicates the end of an expression that's being assigned to a named invocation parameter.
                /// </summary>
                public const string PositionalParameterSplit = ",";
                /// <summary>
                /// Character that indicates the end of an expression that's being assigned to a named invocation parameter.
                /// </summary>
                public const string NamedParameterSplit = ";";
            }

            /// <summary>
            /// Contains the known expression types that can be part of the syntax tree.
            /// </summary>
            public static class SyntaxExpressionTypes
            {
                /// <summary>
                /// Expression that represents the root of an expression tree.
                /// </summary>
                public const string Root = "Root";
                /// <summary>
                /// Expression that wraps another expression.
                /// </summary>
                public const string Wrapped = "Wrapped";
                /// <summary>
                /// Expression that contains an identifier.
                /// </summary>
                public const string Identifier = "Identifier";
            }

            /// <summary>
            /// Contains the known expression types that can be part of a compiled template.
            /// </summary>
            public static class ExpressionTypes
            {
                /// <summary>
                /// Expression that represents a text part of the syntax tree.
                /// </summary>
                public const string Text = "Text";
                /// <summary>
                /// Expression that represents one or multiple expressions used to create generate text.
                /// </summary>
                public const string TemplateBody = "TemplateBody";
                /// <summary>
                /// Expression that represents a comment in a template.
                /// </summary>
                public const string Comment = "Comment";
                /// <summary>
                /// Expression that represents a variable being accessed from the current scope.
                /// </summary>
                public const string Variable = "Variable";
                /// <summary>
                /// Expression that accesses (sub) properties from the result of an expression.
                /// </summary>
                public const string Accessor = "Accessor";
                /// <summary>
                /// Expression that contains a name and a list of expressions that act as an argument for an invocation.
                /// </summary>
                public const string NamedParameter = "NamedParameter";
                /// <summary>
                /// Expression that contains an index and a list of expressions that act as an argument for an invocation.
                /// </summary>
                public const string PositionalParameter = "PositionalParameter";
                /// <summary>
                /// Represents a group of expression that produces a result based on operations in the group.
                /// </summary>
                public const string OperationGroup = "OperationGroup";
                /// <summary>
                /// Expression that represents an operation that is performed on one or more expressions.
                /// </summary>
                public const string Operation = "Operation";
                /// <summary>
                /// Expression that consists of an <see cref="Identifier"/> surrounded by <see cref="TokenTypes.StartExpression"/> and <see cref="TokenTypes.EndExpression"/> and optionally <see cref="TokenTypes.Whitespace"/> tokens in between.
                /// </summary>
                public const string Invocation = "Invocation";
                /// <summary>
                /// Expression that represents an <see cref="Invocation"/> that uses positional parameters. (Like a method call)
                /// </summary>
                public const string PositionalInvocation = "PositionalInvocation";
                /// <summary>
                /// Expression that represents an <see cref="Invocation"/> that uses named parameters. (Identifier=Argument;...)
                /// </summary>
                public const string NamedInvocation = "NamedInvocation";
            }

            /// <summary>
            /// Contains the known parser scopes.
            /// </summary>
            public static class ParserScopes
            {
                /// <summary>
                /// Current expression is being parsed inside a template body (Such as the root, for each body, ...)
                /// </summary>
                public const string TemplateBody = "TemplateBody";
                /// <summary>
                /// Expression is being parsed as the target of an accessor expression.
                /// </summary>
                public const string Accessor = "Accessor";
                /// <summary>
                /// Expression is being parsed as being part of an argument for a parameter expression.
                /// </summary>
                public const string Argument = "Argument";
            }
        }
    }
}
