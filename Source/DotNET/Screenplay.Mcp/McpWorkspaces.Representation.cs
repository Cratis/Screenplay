// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    static object DescribeChange(WorkspaceWriteEntry entry) => new
    {
        entry.Kind,
        documentId = entry.Document.ToString(),
        before = DescribeDocument(entry.Before),
        after = DescribeDocument(entry.After)
    };

    static object? DescribeDocument(WorkspaceDocument? document) => document is null ? null : new
    {
        path = document.Path.Value,
        document.Text,
        bytes = Convert.ToBase64String(document.Bytes.AsSpan())
    };
}
