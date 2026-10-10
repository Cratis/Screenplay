// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_applying_the_first_document_to_a_missing_root : given.a_connection
{
    string _missingRoot = null!;
    JsonElement _applied;
    bool _existedBeforeApply;

    void Establish()
    {
        _missingRoot = Path.Combine(RootPath, "Screenplay");
        Root = new(_missingRoot);
        Connection = new(new McpTools(Root));
        Initialize();
    }

    void Because()
    {
        var opened = Call("open-workspace", new { path = _missingRoot, applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var syntax = new ScreenplayCompiler().Compile(Source);
        var proposal = Call("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "create-document", stableKey = "application", path = "model/application.play", node = SyntaxJson.Serialize(syntax.Value) } }
        }).GetProperty("result").GetProperty("structuredContent");
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        _existedBeforeApply = Directory.Exists(_missingRoot);
        _applied = Call("apply", new
        {
            proposalId = proposal.GetProperty("proposalId").GetString(),
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_not_create_the_root_during_open_or_proposal() => _existedBeforeApply.ShouldBeFalse();
    [Fact] void should_apply_the_first_document() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_create_the_root() => Root.Exists.ShouldBeTrue();
    [Fact] void should_install_source_under_the_root() => new ScreenplayCompiler().Compile(File.ReadAllText(Path.Combine(_missingRoot, "model", "application.play"))).Success.ShouldBeTrue();
    [Fact] void should_install_durable_identity_state() => File.Exists(Path.Combine(_missingRoot, ".screenplay", McpState.FileName)).ShouldBeTrue();
    [Fact] void should_finish_the_recovery_journal() => McpRecoveryJournal.Load(Root).ShouldBeNull();
}
