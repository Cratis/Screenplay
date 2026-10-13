// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_authoring_generation : given.an_authoring_connection
{
    [Theory]
    [InlineData("ast")]
    [InlineData("source")]
    void should_set_change_and_remove_the_strategy_after_review(string transport)
    {
        var opened = Start("concept Code : String\n");
        opened = ReviewAndApply(opened, Propose(opened, transport, new { kind = "GenerationSyntax", strategy = "uuid" }, "uuid"), "generate uuid");
        opened = ReviewAndApply(opened, Propose(opened, transport, new { kind = "GenerationSyntax", strategy = "nanoid", size = 32 }, "nanoid 32"), "generate nanoid 32");
        opened = ReviewAndApply(opened, Propose(opened, transport, new { kind = "GenerationSyntax", strategy = "typeid", prefix = "" }, "typeid \"\""), "generate typeid \"\"");
        opened = ReviewAndApply(opened, Propose(opened, transport, new { kind = "GenerationSyntax", strategy = "typeid", prefix = "user" }, "typeid \"user\""), "generate typeid \"user\"");
        var proposal = Propose(opened, transport, null, null);
        Candidate(proposal).Documents.Single().Text.ShouldEqual("concept Code : String\n");
        ReviewAndApply(opened, proposal, "concept Code : String");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldNotContain("generate");
    }

    [Theory]
    [InlineData("ast", "sequence", null, "PLAY0682")]
    [InlineData("source", "sequence", null, "PLAY0682")]
    [InlineData("ast", "pattern", "ORD-#{7}", "PLAY0268")]
    [InlineData("source", "pattern", "ORD-#{7}", "PLAY0268")]
    void should_preview_refused_forms_with_executable_diagnostics(string transport, string strategy, string? pattern, string code)
    {
        var opened = Start("concept Code : String\n");
        var proposal = Propose(opened, transport, new { kind = "GenerationSyntax", strategy, pattern }, pattern is null ? strategy : $"pattern \"{pattern}\"");
        proposal.GetProperty("after").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
        var review = Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "executable-diagnostics" });
        review.GetProperty("result").GetProperty("items").EnumerateArray().Select(item => item.GetProperty("code").GetString()).ShouldContain(code);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual("concept Code : String\n");
    }

    [Theory]
    [InlineData("ast")]
    [InlineData("source")]
    void should_keep_rules_on_generated_concepts_refused(string transport)
    {
        var opened = Start("concept Code : String\n  validate\n    min 1\n");
        object node = Node("ConceptSyntax", opened.GetProperty("revision").GetString()!).GetProperty("node");
        var original = JsonSerializer.SerializeToNode(node)!.AsObject();
        original["generation"] = JsonSerializer.SerializeToNode(new { kind = "GenerationSyntax", strategy = "nanoid" });
        var proposal = transport == "ast"
            ? Ast(opened, original)
            : SourceProposal(opened, "concept Code : String\n  generate nanoid\n  validate\n    min 1\n");
        var review = Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "executable-diagnostics" });
        proposal.GetProperty("after").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
        review.GetProperty("result").GetProperty("items").EnumerateArray().Select(item => item.GetProperty("code").GetString()).ShouldContain("PLAY0268");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual("concept Code : String\n  validate\n    min 1\n");
    }

    JsonElement Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();

        return Open();
    }

    JsonElement ReviewAndApply(JsonElement opened, JsonElement proposal, string source)
    {
        var candidateSource = Candidate(proposal).Documents.Single().Text;
        candidateSource.ShouldContain(source);
        Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "changes" }).GetProperty("result").GetProperty("items").GetArrayLength().ShouldBeGreaterThan(0);
        var applied = Apply(opened, proposal);
        applied.GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(candidateSource);

        return applied.GetProperty("workspace");
    }

    JsonElement Propose(JsonElement opened, string transport, object? generation, string? strategy) => transport == "ast"
        ? Ast(opened, new { kind = "ConceptSyntax", name = "Code", type = "String", attributes = Array.Empty<string>(), values = Array.Empty<string>(), generation })
        : SourceProposal(opened, "concept Code : String\n" + (strategy is null ? string.Empty : $"  generate {strategy}\n"));

    JsonElement Ast(JsonElement opened, object node) => Result("propose-ast", new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        operations = new[] { new { operation = "replace", target = Node("ConceptSyntax", opened.GetProperty("revision").GetString()!).GetProperty("handle"), node } }
    });

    JsonElement SourceProposal(JsonElement opened, string source) => Result("propose-source", new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents = new[] { new { operation = "replace-document", documentId = Page("documents", opened.GetProperty("revision").GetString()!).EnumerateArray().Single().GetProperty("documentId").GetString(), source } }
    });
}
