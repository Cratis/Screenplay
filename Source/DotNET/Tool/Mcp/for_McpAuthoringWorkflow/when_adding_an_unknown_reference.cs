// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Tool.Mcp.for_McpAuthoringWorkflow;

public class when_adding_an_unknown_reference : given.an_authoring_connection
{
    JsonElement _safe;
    JsonElement _draft;
    JsonElement _applied;
    bool _unchanged;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var command = Node("CommandSyntax", revision);
        var replacement = JsonNode.Parse(command.GetProperty("node").GetRawText())!;
        var property = replacement["properties"]![1]!.DeepClone();
        property["name"] = "notes";
        property["type"]!["name"] = "UnknownProjectName";
        replacement["properties"]!.AsArray().Add(property);
        var arguments = new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = command.GetProperty("handle"), node = replacement } }
        };
        _safe = Call("propose-ast", arguments).GetProperty("result");
        _unchanged = File.ReadAllText(Path.Combine(RootPath, "application.play")) == Source;
        var draftArguments = JsonSerializer.SerializeToNode(arguments)!;
        draftArguments["referencePolicy"] = "Draft";
        _draft = Result("propose-ast", draftArguments);
        _applied = Apply(opened, _draft);
    }

    [Fact] void should_reject_unknown_references_by_default() => _safe.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_explain_the_safe_rejection() => _safe.GetProperty("structuredContent").GetProperty("conflicts").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_preserve_original_source_on_safe_rejection() => _unchanged.ShouldBeTrue();
    [Fact] void should_accept_only_the_explicit_draft_request() => _draft.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_draft_on_the_proposal() => _draft.GetProperty("referencePolicy").GetString().ShouldEqual("Draft");
    [Fact] void should_report_reference_diagnostics() => _draft.GetProperty("authoringDiagnosticCount").GetInt32().ShouldBeGreaterThan(0);
    [Fact] void should_not_claim_executable_readiness() => _draft.GetProperty("after").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_draft_on_apply() => _applied.GetProperty("referencePolicy").GetString().ShouldEqual("Draft");
    [Fact] void should_install_the_explicitly_reviewable_draft() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("UnknownProjectName");
}
