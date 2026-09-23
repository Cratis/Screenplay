// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_creating_a_full_language_application_from_an_empty_folder : given.a_connection
{
    JsonElement _opened;
    JsonElement _proposal;
    JsonElement _applied;
    bool _existedBeforeApply;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        _opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var syntax = new ScreenplayCompiler().Compile($"import External.Customer\n{Source}");
        syntax.Success.ShouldBeTrue();
        _proposal = Call("propose-ast", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "create-document", stableKey = "application", path = "application.play", node = SyntaxJson.Serialize(syntax.Value) } }
        }).GetProperty("result").GetProperty("structuredContent");
        _existedBeforeApply = File.Exists(Path.Combine(RootPath, "application.play"));
        if (!_proposal.GetProperty("success").GetBoolean())
        {
            throw new McpFailure(_proposal.GetRawText());
        }

        _applied = Call("apply", new
        {
            proposalId = _proposal.GetProperty("proposalId").GetString(),
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_open_an_empty_authoring_workspace() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_write_during_proposal() => _existedBeforeApply.ShouldBeFalse();
    [Fact] void should_accept_source_authoring() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_name_the_authoring_validation_contract() => _proposal.GetProperty("validation").GetString().ShouldEqual("Authoring");
    [Fact] void should_not_claim_executable_readiness() => _proposal.GetProperty("after").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_apply_the_complete_new_model() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_compile_the_result_as_full_screenplay() => new ScreenplayCompiler().Compile(File.ReadAllText(Path.Combine(RootPath, "application.play"))).Success.ShouldBeTrue();
    [Fact] void should_keep_default_responses_compact() => _proposal.GetProperty("after").GetProperty("workspaceJson").ValueKind.ShouldEqual(JsonValueKind.Null);
}
