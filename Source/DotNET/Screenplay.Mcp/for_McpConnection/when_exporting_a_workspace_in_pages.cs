// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_exporting_a_workspace_in_pages : given.a_connection
{
    byte[] _expected = [];
    byte[] _exported = [];
    JsonElement _stale;
    int _pages;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace", new { includeContent = true }).GetProperty("result").GetProperty("structuredContent");
        _expected = Encoding.UTF8.GetBytes(opened.GetProperty("workspaceJson").GetString());
        var output = new MemoryStream();
        var offset = 0;
        while (true)
        {
            var page = Call("export-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), offset, limit = 1024 })
                .GetProperty("result").GetProperty("structuredContent");
            output.Write(page.GetProperty("bytesBase64").GetBytesFromBase64());
            _pages++;
            if (page.GetProperty("nextOffset").ValueKind == JsonValueKind.Null)
            {
                break;
            }

            offset = page.GetProperty("nextOffset").GetInt32();
        }

        _exported = output.ToArray();
        output.Dispose();
        _stale = Call("export-workspace", new { expectedRevision = "stale", offset = 0 }).GetProperty("result");
    }

    [Fact] void should_use_multiple_bounded_pages() => _pages.ShouldBeGreaterThan(1);
    [Fact] void should_reconstruct_the_canonical_envelope_exactly() => _exported.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_reopen_the_exported_identity_catalog() => ScreenplayWorkspaceSerializer.Deserialize(_exported).Documents.Single().Text.ShouldEqual(Source);
    [Fact] void should_reject_mixed_revision_pages() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
