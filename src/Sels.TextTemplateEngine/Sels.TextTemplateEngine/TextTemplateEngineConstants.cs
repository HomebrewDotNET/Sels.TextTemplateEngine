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
                public const string Group = "Group";
                /// <summary>
                /// Token that indicates that the sub property might be null and should return the default value of the type if it is.
                /// </summary>
                public const string ElvisOperator = "ElvisOperator";
                /// <summary>
                /// Token that indicates that the sub property should be accessed.
                /// </summary>
                public const string SubPropertyDivisor = "SubPropertyDivisor";
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
                /// Token that indicates the current expression contains a group of other expressions.
                /// </summary>
                public const string ExpressionGroupToken = "*";
                /// <summary>
                /// Character that indicates that a sub property might be null and should return the default value of the type if it is.
                /// </summary>
                public const string ElvisOperator = "?";
                /// <summary>
                /// Character that indicates that a sub property should be accessed.
                /// </summary>
                public const string SubPropertyDivisor = ".";
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
                /// Expression that represents a comment in a template.
                /// </summary>
                public const string Comment = "Comment";
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
            }
        }
    }
}
