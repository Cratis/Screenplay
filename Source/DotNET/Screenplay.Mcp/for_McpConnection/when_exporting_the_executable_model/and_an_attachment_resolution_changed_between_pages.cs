// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_an_attachment_resolution_changed_between_pages : given.a_paged_attachment
{
    System.Text.Json.JsonElement _changed;
    string _refusal = null!;

    void Because()
    {
        File.Delete(Attachment);
        _changed = Content("read-workspace", new { expectedRevision = Revision, view = "implementation-requirements", limit = 1 });
        _refusal = Refusal(new { expectedRevision = Revision, view = "implementation-requirements", offset = 1, expectedAttachmentManifestRevision = Manifest });
    }

    [Fact] void should_report_unresolved_file() => _changed.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("file").GetString() == "Handler.cs").GetProperty("attachmentResolution").GetString().ShouldEqual("UnresolvedFile");
    [Fact] void should_report_empty_content_hash() => _changed.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("file").GetString() == "Handler.cs").GetProperty("contentHash").GetString().ShouldEqual(string.Empty);
    [Fact] void should_change_manifest_revision() => _changed.GetProperty("attachmentManifestRevision").GetString().ShouldNotEqual(Manifest);
    [Fact] void should_refuse_old_pinned_manifest() => _refusal.ShouldContain("StaleRevision");
}
