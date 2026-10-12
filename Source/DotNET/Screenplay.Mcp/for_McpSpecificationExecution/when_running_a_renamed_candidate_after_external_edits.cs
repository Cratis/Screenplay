// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_running_a_renamed_candidate_after_external_edits : given.a_proposal_model
{
    JsonElement _candidate;
    JsonElement _current;
    string _originalId = string.Empty;
    JsonElement _continued;

    void Because()
    {
        _originalId = Content("run-specifications", new { specification = "Projects.Registration.Register.Correct" }).GetProperty("page").GetProperty("items")[0].GetProperty("semanticId").GetString()!;
        var node = Content("read-ast", new { expectedRevision = Revision, kind = "SpecificationSyntax", name = "Correct" }).GetProperty("page").GetProperty("items")[0];
        var proposalId = Content("propose-rename", new
        {
            expectedRevision = Revision,
            expectedCatalogRevision = CatalogRevision,
            target = node.GetProperty("handle"),
            expectedName = "Correct",
            newName = "Renamed",
            validation = "Executable"
        }).GetProperty("proposalId").GetString();
        _candidate = Content("run-specifications", new { proposalId, specification = "Projects.Registration.Register.Renamed" });
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// newer current source\n");
        _continued = Content("run-specifications", new { proposalId, specification = _originalId, expectedSourceRevision = _candidate.GetProperty("sourceRevision").GetString() });
        Content("open-workspace", new { applicationName = "Projects" });
        _current = Content("run-specifications");
    }

    [Fact] void should_execute_the_renamed_candidate() => _candidate.GetProperty("outcome").GetString().ShouldEqual("passed");
    [Fact] void should_use_the_candidate_identity_catalog() => _candidate.GetProperty("page").GetProperty("items")[0].GetProperty("semanticId").GetString().ShouldEqual(_originalId);
    [Fact] void should_keep_the_immutable_candidate_runnable_after_its_base_is_stale() => _continued.GetProperty("outcome").GetString().ShouldEqual("passed");
    [Fact] void should_keep_the_candidate_revision_pinned() => _continued.GetProperty("sourceRevision").GetString().ShouldEqual(_candidate.GetProperty("sourceRevision").GetString());
    [Fact] void should_keep_the_current_specification_name() => _current.GetProperty("page").GetProperty("items")[0].GetProperty("address").GetString().ShouldEqual("Projects.Registration.Register.Correct");
}
