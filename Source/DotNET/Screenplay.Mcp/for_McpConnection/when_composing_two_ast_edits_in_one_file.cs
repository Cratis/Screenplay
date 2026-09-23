// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_composing_two_ast_edits_in_one_file : given.a_connection
{
    JsonElement _proposal;
    JsonElement _applied;
    ApplicationSyntax _application = null!;
    bool _unchangedBeforeApply;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        var catalog = opened.GetProperty("catalogRevision").GetString();
        var command = Node("CommandSyntax", revision);
        var @event = Node("EventSyntax", revision);
        var commandNode = JsonNode.Parse(command.GetProperty("node").GetRawText());
        var eventNode = JsonNode.Parse(@event.GetProperty("node").GetRawText());
        commandNode["description"] = "Creates the project";
        eventNode["file"] = JsonSerializer.SerializeToNode(new { kind = "FileReferenceSyntax", path = "Domain/ProjectRegistered.cs" });
        _proposal = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = catalog,
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[]
            {
                new { operation = "replace", target = command.GetProperty("handle"), node = commandNode },
                new { operation = "replace", target = @event.GetProperty("handle"), node = eventNode }
            }
        }).GetProperty("result").GetProperty("structuredContent");
        _unchangedBeforeApply = File.ReadAllText(Path.Combine(RootPath, "application.play")) == Source;
        if (!_proposal.GetProperty("success").GetBoolean())
        {
            throw new McpFailure(_proposal.GetRawText());
        }

        _applied = Call("apply", new { proposalId = _proposal.GetProperty("proposalId").GetString(), expectedRevision = revision, expectedCatalogRevision = catalog }).GetProperty("result").GetProperty("structuredContent");
        _application = new ScreenplayCompiler().Compile(File.ReadAllText(Path.Combine(RootPath, "application.play"))).Value;
    }

    [Fact] void should_propose_without_touching_disk() => _unchangedBeforeApply.ShouldBeTrue();
    [Fact] void should_compose_both_edits_into_one_document_change() => _proposal.GetProperty("changeCount").GetInt32().ShouldEqual(1);
    [Fact] void should_disclose_canonicalization() => _proposal.GetProperty("canonicalizedSource").GetBoolean().ShouldBeTrue();
    [Fact] void should_apply_the_complete_batch() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_update_the_command() => _application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Description.ShouldEqual("Creates the project");
    [Fact] void should_update_the_event_realization() => _application.Modules.Single().Features.Single().Slices.Single().Events.Single().File.Path.ShouldEqual("Domain/ProjectRegistered.cs");

    JsonElement Node(string kind, string revision) => Call("read-ast", new { expectedRevision = revision, kind, includeContent = true })
        .GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0];
}
