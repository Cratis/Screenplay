// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_expanding_layout;

public class and_unimported_documents_have_reverse_identity_order : given.an_authoring_connection
{
    JsonElement _opened;
    string[] _identityOrder = [];
    string[] _pathOrder = [];
    string[] _modules = [];
    bool _orphansAreUnranked;
    Exception? _error;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "import \"Ordered.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "Ordered.play"), "module First\n  feature Example\n    slice StateView Example\n");
        File.WriteAllText(Path.Combine(RootPath, "Zenith.play"), "module Zenith\n  feature Example\n    slice StateView Example\n");
        File.WriteAllText(Path.Combine(RootPath, "Alpha.play"), "module Alpha\n  feature Example\n    slice StateView Example\n");
        Initialize();
        _opened = Open();
        var workspace = Workspace();
        var orphans = workspace.Documents.Where(document => document.Path.Value.Equals("Alpha.play", StringComparison.Ordinal) || document.Path.Value.Equals("Zenith.play", StringComparison.Ordinal)).ToArray();
        _identityOrder = [.. orphans.OrderBy(document => document.Id.ToString(), StringComparer.Ordinal).Select(document => document.Path.Value)];
        _pathOrder = [.. orphans.OrderBy(document => document.Path.Value, StringComparer.Ordinal).Select(document => document.Path.Value)];
        var ranks = WorkspaceTimelineRepairs.Timeline(workspace).Ranks;
        _orphansAreUnranked = !ranks.ContainsKey(AuthoredOrder.Key(["Alpha"])) && !ranks.ContainsKey(AuthoredOrder.Key(["Zenith"]));
    }

    void Because() => _error = Catch.Exception(() =>
    {
        var proposal = Result("expand-layout", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            layout = "slice",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        Apply(_opened, proposal);
        var compilation = new PlayFileCompiler().CompileApplication(Path.Combine(RootPath, "application.play"));
        _modules = [.. compilation.Result.Value!.Modules.Select(module => module.Name)];
    });

    [Fact] void should_exercise_identity_order_opposite_to_path_order() => _identityOrder.ShouldEqual([.. _pathOrder.Reverse()]);
    [Fact] void should_exercise_siblings_the_ordering_root_never_reaches() => _orphansAreUnranked.ShouldBeTrue();
    [Fact] void should_admit_the_valid_expansion() => _error.ShouldBeNull();
    [Fact] void should_keep_unranked_modules_in_path_order_after_ranked_modules() => _modules.ShouldEqual(["First", "Alpha", "Zenith"]);
}
