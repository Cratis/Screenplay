// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reviewing_a_diagnostic_repair : given.an_authoring_connection
{
    JsonElement _repair;
    JsonElement _proposal;
    JsonElement _review;
    JsonElement _applied;
    bool _unchanged;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("        produces ProjectRegistered", "        validate csharp\n          ```\n          yield return \"invalid\";\n          ```\n        produces ProjectRegistered", StringComparison.Ordinal));
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        _repair = Result("read-workspace", new { expectedRevision = revision, view = "repairs" })
            .GetProperty("page").GetProperty("items").EnumerateArray().Single();
        _proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = _repair.GetProperty("diagnosticCode").GetString(),
            subject = _repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        _unchanged = File.ReadAllText(Path.Combine(RootPath, "application.play")).Contains("validate csharp", StringComparison.Ordinal);
        _review = Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "changes" });
        _applied = Apply(opened, _proposal);
    }

    [Fact] void should_expose_a_typed_operation_and_subject() =>
        (_repair.GetProperty("operations").EnumerateArray().Single().GetProperty("operation").GetString() == "replace" &&
         _repair.GetProperty("subject").GetProperty("path").GetString()!.Length > 0).ShouldBeTrue();
    [Fact] void should_not_write_before_explicit_apply() => _unchanged.ShouldBeTrue();
    [Fact] void should_retain_a_reviewable_proposal() => _review.GetProperty("result").GetProperty("items").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_apply_only_the_reviewed_plan() =>
        (_applied.GetProperty("success").GetBoolean() && File.ReadAllText(Path.Combine(RootPath, "application.play")).Contains("```csharp", StringComparison.Ordinal)).ShouldBeTrue();
}
