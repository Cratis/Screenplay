// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_external_text_preserves_identity_addresses : given.a_durable_workspace
{
    JsonElement _opened;
    byte[] _expected = [];

    void Establish()
    {
        _expected = Encoding.UTF8.GetBytes(Source.Replace("Registers a new project", "Registers the project", StringComparison.Ordinal));
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), _expected);
    }

    void Because() => _opened = Result(new McpWorkspaces(Root).Open(McpJson.Empty));

    [Fact] void should_preserve_the_catalog() => _opened.GetProperty("catalogRevision").GetString().ShouldEqual(Original.IdentityCatalog.Revision.ToString());
    [Fact] void should_open_the_current_exact_bytes() => McpState.Deserialize(OriginalState).Open(Root).Documents.Single().Bytes.AsSpan().SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_leave_metadata_untouched() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
}
