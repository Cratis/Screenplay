// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_an_attachment_body_changed_between_pages : given.a_paged_attachment
{
    System.Text.Json.JsonElement _changed;
    System.Text.Json.JsonElement _legacy;
    System.Text.Json.JsonElement _pinned;
    string _modelRefusal = null!;
    string _requirementRefusal = null!;

    void Because()
    {
        File.WriteAllText(Attachment, "second");
        _changed = Content("read-workspace", new { expectedRevision = Revision, view = "executable-model", limit = 1 });
        _modelRefusal = Refusal(new { expectedRevision = Revision, view = "executable-model", offset = 1, expectedModelRevision = Model, expectedAttachmentManifestRevision = Manifest });
        _requirementRefusal = Refusal(new { expectedRevision = Revision, view = "implementation-requirements", offset = 1, expectedAttachmentManifestRevision = Manifest });
        _legacy = Content("read-workspace", new { expectedRevision = Revision, view = "implementation-requirements", offset = 1 });
        _pinned = Content("read-workspace", new { expectedRevision = Revision, view = "implementation-requirements", offset = 1, expectedAttachmentManifestRevision = _changed.GetProperty("attachmentManifestRevision").GetString() });
    }

    [Fact] void should_preserve_model_revision() => _changed.GetProperty("modelRevision").GetString().ShouldEqual(Model);
    [Fact] void should_preserve_workspace_revision() => _changed.GetProperty("workspace").GetProperty("revision").GetString().ShouldEqual(Revision);
    [Fact] void should_change_manifest_revision() => _changed.GetProperty("attachmentManifestRevision").GetString().ShouldNotEqual(Manifest);
    [Fact] void should_refuse_stale_model_page() => _modelRefusal.ShouldContain("StaleRevision");
    [Fact] void should_refuse_stale_pinned_requirements_page() => _requirementRefusal.ShouldContain("StaleRevision");
    [Fact] void should_allow_legacy_unpinned_requirements_page() => _legacy.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_return_manifest_for_legacy_continuation() => _legacy.GetProperty("attachmentManifestRevision").GetString().ShouldEqual(_changed.GetProperty("attachmentManifestRevision").GetString());
    [Fact] void should_allow_current_pinned_requirements_page() => _pinned.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
}
