using System.Collections.Generic;
using System.Linq;

namespace SeleniumBrowsersPool.Helpers
{
    internal static class CollectionExtensions
    {
        internal static IEnumerable<T> NullIfEmpty<T>(this IEnumerable<T> collection)
            => collection?.Any() ?? false ? collection : null;
    }
}
