// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_a_continuation_is_stale_or_unpinned : given.an_export
{
    string _staleWorkspace = null!;
    string _missingModel = null!;
    string _missingManifest = null!;
    string _staleModel = null!;
    string _staleManifest = null!;

    void Establish() => Initialize();

    void Because()
    {
        var revision = Content("open-workspace", new { }).GetProperty("revision").GetString();
        var first = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 1 });
        var model = first.GetProperty("modelRevision").GetString();
        var manifest = first.GetProperty("attachmentManifestRevision").GetString();
        _staleWorkspace = Refusal(new { expectedRevision = "stale", view = "executable-model", offset = 1, expectedModelRevision = model, expectedAttachmentManifestRevision = manifest });
        _missingModel = Refusal(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedAttachmentManifestRevision = manifest });
        _missingManifest = Refusal(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = model });
        _staleModel = Refusal(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = "stale", expectedAttachmentManifestRevision = manifest });
        _staleManifest = Refusal(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = model, expectedAttachmentManifestRevision = "stale" });
    }

    [Fact] void should_refuse_stale_workspace() => _staleWorkspace.ShouldContain("StaleRevision");
    [Fact] void should_require_model_revision() => _missingModel.ShouldContain("expectedModelRevision");
    [Fact] void should_require_manifest_revision() => _missingManifest.ShouldContain("expectedAttachmentManifestRevision");
    [Fact] void should_refuse_stale_model() => _staleModel.ShouldContain("StaleRevision");
    [Fact] void should_refuse_stale_manifest() => _staleManifest.ShouldContain("StaleRevision");
}
