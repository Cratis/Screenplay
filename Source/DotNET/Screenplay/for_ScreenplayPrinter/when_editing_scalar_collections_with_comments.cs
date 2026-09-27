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
    void should_keep_comments_when_editing_one_scalar_entry_in_place()
    {
        var parsed = _compiler.Parse(Source).Value!;
        var edited = parsed with
        {
            Concepts = [parsed.Concepts.Single() with { Values = ["active", "waiting", "closed"] }],
            Personas = [parsed.Personas!.Single() with { Policies = ["View", "Approve"] }],
            Themes = [parsed.Themes!.Single() with { CompatibleWith = ["core", "widgets"] }],
            UiProfiles = [parsed.UiProfiles!.Single() with { Packages = ["core", "widgets"] }]
        };

        var printed = _printer.Print(edited);

        printed.ShouldContain("// pending lead\n  waiting // pending note");
        printed.ShouldContain("policy Approve // edit note");
        printed.ShouldContain("compatible with widgets // forms note");
        printed.ShouldContain("widgets // package forms note");
        printed.ShouldContain("active // active note");
        _compiler.Parse(printed).Diagnostics.ShouldBeEmpty();
        _printer.Print(_compiler.Parse(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_comments_when_editing_a_role_or_split_target_in_place()
    {
        const string specification = "specification Access\n  given caller\n    role \"Editor\" // editor note\n    // viewer lead\n    role \"Viewer\" // viewer note\n  when Submit\n";
        var parsedSpecification = _compiler.CompileSpecification(specification).Value!;
        var editedSpecification = parsedSpecification with
        {
            GivenCaller = parsedSpecification.GivenCaller! with { Roles = ["Editor", "Auditor"] }
        };
        var printedSpecification = _printer.Print(editedSpecification);
        printedSpecification.ShouldContain("// viewer lead\n    role \"Auditor\" // viewer note");
        _printer.Print(_compiler.CompileSpecification(printedSpecification).Value!).ShouldEqual(printedSpecification);

        const string capture = "capture Import\n  map\n    split name by \",\"\n      first // first note\n      // second lead\n      second // second note\n  append Imported\n";
        var parsedCapture = _compiler.CompileCapture(capture).Value!;
        var split = parsedCapture.Map.OfType<CaptureSplitSyntax>().Single();
        var editedCapture = parsedCapture with { Map = [split with { Targets = ["first", "other"] }] };
        var printedCapture = _printer.Print(editedCapture);
        printedCapture.ShouldContain("// second lead\n      other // second note");
        _printer.Print(_compiler.CompileCapture(printedCapture).Value!).ShouldEqual(printedCapture);
    }

    [Fact]
    void should_drop_comments_when_the_collection_slot_is_removed()
    {
        var parsed = _compiler.Parse(Source).Value!;
        var edited = parsed with { Concepts = [parsed.Concepts.Single() with { Values = ["active", "closed"] }] };

        var printed = _printer.Print(edited);

        printed.ShouldNotContain("pending lead");
        printed.ShouldNotContain("pending note");
        printed.ShouldContain("closed // closed note");
    }

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
    void should_keep_each_duplicate_comment_when_editing_one_occurrence_in_place()
    {
        const string source = "concept Status : Enum\n  open // first note\n  open // second note\n";
        var parsed = _compiler.Parse(source).Value!;
        var concept = parsed.Concepts.Single();
        var edited = parsed with { Concepts = [concept with { Values = ["waiting", "open"] }] };

        var printed = _printer.Print(edited);

        printed.ShouldContain("waiting // first note");
        printed.ShouldContain("open // second note");
        _printer.Print(_compiler.Parse(printed).Value!).ShouldEqual(printed);
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
    void should_report_removed_comments_but_not_in_place_edits_in_workspace_diffs()
    {
        var bytes = Encoding.UTF8.GetBytes("concept Status : Enum\n  active // active note\n  pending // pending note\n  closed // closed note\n");
        var document = WorkspaceDocument.Create("status", PortablePlayPath.Parse("Status.play"), bytes);
        var workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(candidate => candidate.Node is ConceptSyntax);
        var concept = (ConceptSyntax)entry.Node;

        WorkspaceAuthoringResult ReplaceValues(params string[] values) => workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, concept, concept with { Values = values })]
        });

        var edited = ReplaceValues("active", "waiting", "closed");
        edited.Accepted.ShouldBeTrue();
        edited.WritePlan!.Entries.Single().After!.Text.ShouldContain("waiting // pending note");
        WorkspaceDroppedComments.In(edited.WritePlan).ShouldBeEmpty();

        var removed = ReplaceValues("active", "closed");
        removed.Accepted.ShouldBeTrue();
        var dropped = WorkspaceDroppedComments.In(removed.WritePlan!);
        dropped.Length.ShouldEqual(1);
        dropped[0].Text.ShouldContain("pending note");
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
