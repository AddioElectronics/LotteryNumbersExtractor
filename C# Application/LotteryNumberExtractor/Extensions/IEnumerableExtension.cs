using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Addio.Universal.NET.Standard.Extensions
{
    /// <summary>
    /// A class containing extension methods to extend the <see cref="IEnumerable{T}"/> functionality.
    /// </summary>
    public static partial class IEnumerableExtension
    {

        /// <inheritdoc cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
        /// <remarks>Compares if either <see cref="IEnumerable{T}"/> are null.</remarks>
        public static bool SequenceEqualNullable<T>(this IEnumerable<T> first, IEnumerable<T> last)
        {
            if (ReferenceEquals(first, null))
                return !ReferenceEquals(last, null);

            if (ReferenceEquals(last, null))
                return false;

            return first.SequenceEqual(last);

        }

        /// <inheritdoc cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
        /// <remarks>Compares if either <see cref="IEnumerable{T}"/> are null.</remarks>
        public static bool SequenceEqualNullable<T>(this IEnumerable<T> first, IEnumerable<T> last, IEqualityComparer<T> comparer)
        {
            if (ReferenceEquals(first, null))
                return !ReferenceEquals(last, null);

            if (ReferenceEquals(last, null))
                return false;

            return first.SequenceEqual(last, comparer);

        }


    }

}
