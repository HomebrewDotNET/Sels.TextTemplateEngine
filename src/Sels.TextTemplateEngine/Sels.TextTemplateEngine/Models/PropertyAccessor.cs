using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sels.TextTemplateEngine
{
    /// <summary>
    /// Contains information about a property being accessed from the result of an expression.
    /// </summary>
    public struct PropertyAccessor
    {
        /// <summary>
        /// The name of the property being acessed.
        /// </summary>
        public string Name { get; init; }
        /// <summary>
        /// If the current property can return null. Will return null instead of throwing <see cref="NullReferenceException"/> when accessing a sub property.
        /// </summary>
        public bool IsNullable { get; init; }
    }
}
