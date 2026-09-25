// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_proposing_a_diagnostic_repair;

public class and_the_revision_is_current : given.a_repairable_workspace
{
    JsonElement _proposal;
    JsonElement _review;
    JsonElement _applied;
    byte[] _before = null!;
    byte[] _after = null!;
    byte[] _beforeApply = null!;
    byte[] _appliedBytes = null!;

    void Because()
    {
        _before = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        _proposal = Result("propose-repair", Arguments());
        var id = _proposal.GetProperty("proposalId").GetString();
        _review = Result("read-proposal", new { proposalId = id, view = "changes" });
        var bytes = Result("read-proposal", new { proposalId = id, view = "after", documentId = Repair.GetProperty("subject").GetProperty("documentId").GetString() });
        _after = bytes.GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        _beforeApply = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        _applied = Apply(Opened, _proposal);
        _appliedBytes = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
    }

    [Fact] void should_expose_a_typed_operation() => Repair.GetProperty("operations").EnumerateArray().Single().GetProperty("operation").GetString().ShouldEqual("replace");
    [Fact] void should_expose_a_subject() => Repair.GetProperty("subject").GetProperty("path").GetString()!.Length.ShouldBeGreaterThan(0);
    [Fact] void should_retain_a_reviewable_proposal() => _review.GetProperty("result").GetProperty("items").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_not_write_during_proposal() => _beforeApply.SequenceEqual(_before).ShouldBeTrue();
    [Fact] void should_apply_successfully() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_print_the_tagged_fence() => System.Text.Encoding.UTF8.GetString(_appliedBytes).ShouldContain("```csharp");
    [Fact] void should_remove_the_legacy_header() => System.Text.Encoding.UTF8.GetString(_appliedBytes).ShouldNotContain("validate csharp");
    [Fact] void should_apply_exactly_the_reviewed_bytes() => _appliedBytes.SequenceEqual(_after).ShouldBeTrue();
}
