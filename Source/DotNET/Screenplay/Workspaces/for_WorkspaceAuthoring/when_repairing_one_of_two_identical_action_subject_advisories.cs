// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_repairing_one_of_two_identical_action_subject_advisories : Specification
{
    const string Source = """
        concept ItemId : Uuid
        module Work
          feature Items
            slice StateView List
              readmodel Row
                itemId ItemId key
              command Retry
                itemId ItemId
              query Rows => Row[]
              screen First
                data Row[] via query Rows
                action Retry // first invocation
              screen Second
                data Row[] via query Rows
                action Retry // second invocation
        """;
    ScreenplayWorkspace _workspace;
    WorkspaceSyntaxIndex _index;

    void Establish()
    {
        _workspace = ScreenplayWorkspace.Create("Model", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model")));
        _index = WorkspaceSyntaxIndex.Create(_workspace);
    }

    [Theory]
    [InlineData("First", "Second")]
    [InlineData("Second", "First")]
    void should_repair_only_the_selected_occurrence(string selectedName, string untouchedName)
    {
        var advisories = _workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InferredActionSubject).ToArray();
        advisories.Length.ShouldEqual(2);
        advisories[0].Message.ShouldEqual(advisories[1].Message);
        (advisories[0].Location != advisories[1].Location).ShouldBeTrue();
        var selected = Action(_index, selectedName);
        var untouched = Action(_index, untouchedName);
        var diagnostic = _index.RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.InferredActionSubject && value.Location == selected.Location);
        var repair = WorkspaceDiagnosticRepairs.Find(_index, _workspace.Revision, diagnostic).Single();
        repair.CanFixAll.ShouldBeFalse();
        _index.Find(repair.Subject)!.Location.ShouldEqual(selected.Location);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, repair, new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });

        result.Accepted.ShouldBeTrue();
        var candidate = result.Workspace!;
        var afterIndex = WorkspaceSyntaxIndex.Create(candidate);
        var repaired = Action(afterIndex, selectedName);
        var remaining = Action(afterIndex, untouchedName);
        repaired.Subject.ShouldEqual(ScreenActionSubjectKind.Screen);
        remaining.Subject.ShouldBeNull();
        afterIndex.Entries.Select(entry => entry.Node).OfType<ScreenActionSyntax>().Count(action => action.Subject is not null).ShouldEqual(1);
        SyntaxJson.StructurallyEqual(repaired, selected with { Subject = ScreenActionSubjectKind.Screen }).ShouldBeTrue();
        SyntaxJson.StructurallyEqual(remaining, untouched).ShouldBeTrue();
        var remainingAdvisory = candidate.Compilation.Diagnostics.Single(value => value.Code == DiagnosticCodes.InferredActionSubject);
        remainingAdvisory.Message.ShouldEqual(diagnostic.Message);
        remainingAdvisory.Location.ShouldEqual(remaining.Location);
        candidate.Compilation.Diagnostics.Any(value => value.Code == DiagnosticCodes.ChangedActionSubject && value.Location == repaired.Location).ShouldBeFalse();

        var expected = _workspace.Compilation.Diagnostics
            .Where(value => value.Location != selected.Location || value.Code is not (DiagnosticCodes.InferredActionSubject or DiagnosticCodes.ChangedActionSubject))
            .Select(value => (value.Code, value.Message)).Order().ToArray();
        var actual = candidate.Compilation.Diagnostics.Select(value => (value.Code, value.Message)).Order().ToArray();
        actual.SequenceEqual(expected).ShouldBeTrue();
        SemanticModelSerializer.Serialize(candidate.Compilation.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_workspace.Compilation.Value!.Model)).ShouldBeTrue();
        candidate.IdentityCatalog.Revision.ShouldEqual(_workspace.IdentityCatalog.Revision);
        _workspace.Documents.Single().Text.ShouldEqual(Source);
        Action(_index, selectedName).Subject.ShouldBeNull();
        Action(_index, untouchedName).Subject.ShouldBeNull();
        SyntaxJson.StructurallyEqual(Action(_index, untouchedName), untouched).ShouldBeTrue();
        candidate.Documents.Single().Text.ShouldContain("// first invocation");
        candidate.Documents.Single().Text.ShouldContain("// second invocation");
    }

    static ScreenActionSyntax Action(WorkspaceSyntaxIndex index, string screen) => index.Entries
        .Select(entry => entry.Node).OfType<ScreenSyntax>().Single(node => node.Name == screen)
        .Directives.OfType<ScreenActionSyntax>().Single();
}
