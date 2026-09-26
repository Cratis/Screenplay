// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_editing_scalar_collections_with_comments : given.a_printer
{
    const string Source = """
        concept Status : Enum
          active // active note
          // pending lead
          pending // pending note
          closed // closed note
        persona Clerk
          policy View // view note
          policy Edit // edit note
        theme Aurora
          compatible with core // core note
          compatible with forms // forms note
        ui profile Desktop
          packages
            core // package core note
            forms // package forms note
        """;

    [Fact]
    void should_follow_reordered_inserted_and_removed_enum_values()
    {
        var parsed = _compiler.Parse(Source).Value!;
        var concept = parsed.Concepts.Single();
        var edited = parsed with { Concepts = [concept with { Values = ["closed", "new", "active"] }] };

        var printed = _printer.Print(edited);

        printed.ShouldContain("closed // closed note");
        printed.ShouldContain("active // active note");
        printed.ShouldNotContain("new //");
        printed.ShouldNotContain("new // pending note");
        printed.ShouldNotContain("active // pending note");
        printed.ShouldNotContain("closed // active note");
        printed.ShouldNotContain("pending lead");
        printed.ShouldNotContain("pending note");
    }

    [Fact]
    void should_follow_values_in_each_other_scalar_collection()
    {
        var parsed = _compiler.Parse(Source).Value!;
        var edited = parsed with
        {
            Personas = [parsed.Personas!.Single() with { Policies = ["Edit", "New", "View"] }],
            Themes = [parsed.Themes!.Single() with { CompatibleWith = ["forms", "new", "core"] }],
            UiProfiles = [parsed.UiProfiles!.Single() with { Packages = ["forms", "new", "core"] }]
        };

        var printed = _printer.Print(edited);

        printed.ShouldContain("policy Edit // edit note");
        printed.ShouldContain("policy View // view note");
        printed.ShouldContain("compatible with forms // forms note");
        printed.ShouldContain("compatible with core // core note");
        printed.ShouldContain("forms // package forms note");
        printed.ShouldContain("core // package core note");
        printed.ShouldNotContain("new //");
    }

    [Fact]
    void should_follow_specification_roles_when_reordered()
    {
        const string source = """
            specification Access
              given caller
                role "Editor" // editor note
                role "Viewer" // viewer note
              when Submit
            """;
        var parsed = _compiler.CompileSpecification(source).Value!;
        var edited = parsed with { GivenCaller = parsed.GivenCaller! with { Roles = ["Viewer", "New", "Editor"] } };

        var printed = _printer.Print(edited);

        printed.ShouldContain("role \"Viewer\" // viewer note");
        printed.ShouldContain("role \"Editor\" // editor note");
        printed.ShouldNotContain("role \"New\" //");
    }

    [Fact]
    void should_follow_capture_targets_when_reordered()
    {
        const string source = """
            capture Import
              map
                split name by ","
                  first // first note
                  second // second note
              append Imported
            """;
        var parsed = _compiler.CompileCapture(source).Value!;
        var split = parsed.Map.OfType<CaptureSplitSyntax>().Single();
        var edited = parsed with { Map = [split with { Targets = ["second", "new", "first"] }] };

        var printed = _printer.Print(edited);

        printed.ShouldContain("second // second note");
        printed.ShouldContain("first // first note");
        printed.ShouldNotContain("new //");
    }

    [Fact]
    void should_keep_separate_comments_for_duplicate_enum_values()
    {
        const string source = """
            concept Status : Enum
              open // first note
              open // second note
            """;
        var parsed = _compiler.Parse(source).Value!;

        var printed = _printer.Print(parsed);

        printed.ShouldContain("open // first note");
        printed.ShouldContain("open // second note");
    }

    [Fact]
    void should_follow_values_through_a_workspace_replacement()
    {
        var bytes = Encoding.UTF8.GetBytes("concept Status : Enum\n  active // active note\n  pending // pending note\n  closed // closed note\n");
        var document = WorkspaceDocument.Create("status", PortablePlayPath.Parse("Status.play"), bytes);
        var workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(candidate => candidate.Node is ConceptSyntax);
        var concept = (ConceptSyntax)entry.Node;
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, concept, concept with { Values = ["closed", "new", "active"] })]
        });

        result.Accepted.ShouldBeTrue();
        var text = result.WritePlan!.Entries.Single().After!.Text;
        text.ShouldContain("closed // closed note");
        text.ShouldContain("active // active note");
        text.ShouldNotContain("new //");
        text.ShouldNotContain("new // pending note");
    }
}
