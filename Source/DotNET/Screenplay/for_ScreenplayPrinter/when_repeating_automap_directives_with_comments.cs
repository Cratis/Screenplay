// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_repeating_automap_directives_with_comments : given.a_printer
{
    const string Source = """
        projection Orders => OrderList
          automap // projection first
          no automap // projection second
          from OrderPlaced
          every
            automap // every first
            no automap // every second
          all
            automap // all first
            no automap // all second
          children items identified by id
            automap // children first
            no automap // children second
            from ItemAdded
          nested details
            automap // nested first
            no automap // nested second
            from DetailAdded
          join details on id
            with DetailsJoined
              automap // join first
              no automap // join second
        """;

    [Fact]
    void should_warn_for_each_repeated_automap_and_keep_each_comment_separate()
    {
        var parsed = _compiler.CompileProjection(Source);
        var printed = _printer.Print(parsed.Value!);
        var reparsed = _compiler.CompileProjection(printed);

        parsed.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedProjectionAutoMap).ShouldEqual(6);
        reparsed.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedProjectionAutoMap).ShouldEqual(6);
        foreach (var scope in new[] { "projection", "every", "all", "children", "nested", "join" })
        {
            printed.ShouldContain($"automap // {scope} first");
            printed.ShouldContain($"no automap // {scope} second");
            printed.ShouldNotContain($"no automap // {scope} first // {scope} second");
        }

        _printer.Print(reparsed.Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_anchor_removed_automap_comments_to_the_projection_body()
    {
        const string source = """
            projection Orders => OrderList
              automap // projection first
              no automap // projection second
              from OrderPlaced
            """;
        var original = _compiler.CompileProjection(source);
        var printed = _printer.Print(original.Value! with { AutoMap = AutoMapMode.Inherit });
        var reparsed = _compiler.CompileProjection(printed);

        printed.ShouldContain("  // projection first\n  // projection second\n  from OrderPlaced");
        printed.ShouldNotContain("from OrderPlaced // projection first");
        _printer.Print(reparsed.Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_repeated_automap_lines_and_comments_after_an_unrelated_workspace_edit()
    {
        const string source = """
            module Shop
              feature Orders
                slice StateView OrderList
                  projection Orders => OrderList
                    automap // projection first
                    no automap // projection second
                    from OrderPlaced
            """;
        var document = WorkspaceDocument.Create("orders", PortablePlayPath.Parse("Orders.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
        var rootEntry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Parent is null);
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new AddWorkspaceNode(rootEntry.Handle, rootEntry.Node, "imports", new ImportSyntax("External.Unused", SourceLocation.Start))]
        });

        result.Accepted.ShouldBeTrue();
        var printed = result.WritePlan!.Entries.Single().After!.Text;
        printed.ShouldContain("projection Orders => OrderList");
        printed.ShouldContain("automap // projection first\n        no automap // projection second");
        var reparsed = _compiler.Compile(printed);
        _printer.Print(reparsed.Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_not_reprint_previous_automap_settings_after_a_typed_mode_edit()
    {
        var parsed = _compiler.CompileProjection(Source);
        var edited = parsed.Value! with { AutoMap = AutoMapMode.Enabled };
        var printed = _printer.Print(edited);
        var reparsed = _compiler.CompileProjection(printed);

        printed.Split('\n').Count(line => line.StartsWith("  automap", StringComparison.Ordinal)).ShouldEqual(1);
        printed.ShouldNotContain("  no automap // projection second");
        printed.ShouldContain("automap // projection second\n  // projection first");
        reparsed.Value!.AutoMap.ShouldEqual(AutoMapMode.Enabled);
        _printer.Print(reparsed.Value).ShouldEqual(printed);
    }
}
