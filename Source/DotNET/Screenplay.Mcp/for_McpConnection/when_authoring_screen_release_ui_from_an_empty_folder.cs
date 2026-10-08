// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_authoring_screen_release_ui_from_an_empty_folder : given.a_connection
{
    JsonElement _schema;
    JsonElement _opened;
    JsonElement _proposal;
    JsonElement _applied;
    string _source = string.Empty;
    bool _existedBeforeApply;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        _schema = Call("syntax-schema", new { kind = "UiBindingSyntax" }).GetProperty("result").GetProperty("structuredContent");
        _opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        _source = File.ReadAllText(Path.Combine(RepositoryRoot(), "Documentation", "screenplay", "fixtures", "screen-release-ui.play"));
        var syntax = new ScreenplayCompiler().Compile(_source);
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

    [Fact] void should_discover_the_typed_binding_schema() => _schema.GetRawText().ShouldContain("bindingKind");
    [Fact] void should_open_an_empty_authoring_workspace() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_write_during_proposal() => _existedBeforeApply.ShouldBeFalse();
    [Fact] void should_author_the_release_fixture_with_revision_checks() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_apply_the_release_fixture() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_typed_component_bindings_on_disk() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("from component invoices.selectedItem");

    static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
