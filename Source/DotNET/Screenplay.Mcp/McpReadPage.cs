// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

sealed record McpReadPage<T>(int Total, int Matched, int Offset, int Limit, bool Truncated, int? NextOffset, IEnumerable<T> Items)
{
    internal static McpReadPage<T> Create(IEnumerable<T> source, Func<T, bool> predicate, int offset, int limit)
    {
        if (offset < 0 || limit is < 1 or > 200)
        {
            throw new McpFailure("offset must be nonnegative and limit must be between 1 and 200.");
        }

        var all = source.ToArray();
        var matched = all.Where(predicate).ToArray();
        var items = matched.Skip(offset).Take(limit).ToArray();
        var hasMore = offset < matched.Length && items.Length < matched.Length - offset;

        return new(all.Length, matched.Length, offset, limit, hasMore, hasMore ? offset + items.Length : null, items);
    }
}
