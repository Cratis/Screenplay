// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

static class McpPaging
{
    internal static McpPage<T> Page<T>(IEnumerable<T> values, JsonElement arguments, string revision) =>
        Page(values, value => value, arguments, revision);

    internal static McpPage<TResult> Page<TSource, TResult>(IEnumerable<TSource> values, Func<TSource, TResult> project, JsonElement arguments, string revision)
    {
        var items = values.ToArray();
        var offset = McpJson.Integer(arguments, "offset", 0, 0, items.Length);
        var limit = McpJson.Integer(arguments, "limit", 50, 1, 200);
        var page = items.Skip(offset).Take(limit).Select(project).ToArray();
        return new(revision, items.Length, offset, page, offset + page.Length < items.Length ? offset + page.Length : null);
    }

    // Leave space for workspace metadata and the MCP text/structured copies, including JSON escaping.
    internal static McpPage<object> BoundedSourcePage(IEnumerable<object> values, JsonElement arguments, string revision)
    {
        const int budget = 192 * 1024;
        var items = values.ToArray();
        var offset = McpJson.Integer(arguments, "offset", 0, 0, items.Length);
        var limit = McpJson.Integer(arguments, "limit", 50, 1, 200);
        var page = new List<object>();
        var bytes = 0;
        foreach (var item in items.Skip(offset).Take(limit))
        {
            var size = JsonSerializer.SerializeToUtf8Bytes(item, McpJson.Options).Length + 1;
            if (size > budget && page.Count == 0)
                throw new McpFailure("ResponseTooLarge: a single source inventory item exceeds its byte budget. Smaller count pages cannot reduce it. Read the original path with read-document byte pages or select narrower read-ast children; no identity or value was truncated.") { FailureKind = "LimitExceeded" };
            if (bytes + size > budget) break;
            page.Add(item);
            bytes += size;
        }

        return new(revision, items.Length, offset, [.. page], offset + page.Count < items.Length ? offset + page.Count : null);
    }

    internal static object Bytes(byte[] bytes, JsonElement arguments, string revision)
    {
        var offset = McpJson.Integer(arguments, "offset", 0, 0, bytes.Length);
        var limit = McpJson.Integer(arguments, "limit", 48 * 1024, 1, 192 * 1024);
        var length = Math.Min(limit, bytes.Length - offset);
        return new
        {
            revision,
            totalBytes = bytes.Length,
            offset,
            byteCount = length,
            bytesBase64 = Convert.ToBase64String(bytes, offset, length),
            nextOffset = offset + length < bytes.Length ? (int?)(offset + length) : null
        };
    }
}
