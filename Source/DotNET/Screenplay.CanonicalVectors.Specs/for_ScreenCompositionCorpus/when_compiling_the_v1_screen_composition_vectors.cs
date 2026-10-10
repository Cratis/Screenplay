// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

public class when_compiling_the_v1_screen_composition_vectors : Specification
{
    CanonicalScreenCorpusVector _corpus;
    CompilationResult<ApplicationSyntax> _positive;
    Dictionary<string, string[]> _rejections;

    void Because()
    {
        _corpus = ScreenCompositionCorpus.V1;
        _positive = new ScreenplayCompiler().Compile(_corpus.TypedSourceCases.Single().Document.Text);
        _rejections = _corpus.RejectionVectors.ToDictionary(
            vector => vector.Name,
            vector => new ScreenplayCompiler().Compile(vector.SourceForm.Documents.Single().Text).Diagnostics
                .OrderBy(diagnostic => diagnostic.Location.Line)
                .ThenBy(diagnostic => diagnostic.Location.Column)
                .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
                .ToArray());
    }

    [Fact] void should_compile_the_positive_composition_without_diagnostics() => _positive.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_screen_contributions() => Screens(_positive.Value!).SelectMany(screen => screen.Contributions).Count().ShouldEqual(2);
    [Fact] void should_carry_document_level_exposures() => _positive.Value!.Exposures.Select(exposure => exposure.Owner).ShouldEqual(["WorkspaceFeatureShell", "EditWorkItemDialog"]);
    [Fact] void should_carry_instance_contributions() => _positive.Value!.InstanceContributions.Single().Contributions.Last().Items.Select(item => item.Id).ShouldEqual(["export:csv", "archive"]);
    [Fact] void should_carry_template_content() => _positive.Value!.Modules.Single().ScreenTemplates.Single().Content.Single().Slot.ShouldEqual("header");
    [Fact] void should_carry_dialog_template_content() => _positive.Value!.Modules.Single().DialogTemplates!.Single().Content.Single().Slot.ShouldEqual("actions");
    [Fact] void should_carry_grid_dimensions() => Grid(_positive.Value!).Columns.ShouldEqual(2);
    [Fact] void should_cover_every_compatibility_and_reference_rejection() => _corpus.RejectionVectors.Length.ShouldEqual(6);
    [Fact] void should_expect_no_artifacts_from_any_rejection() => _corpus.RejectionVectors.All(vector => vector.ArtifactPaths.IsEmpty).ShouldBeTrue();

    [Fact]
    void should_report_exactly_the_pinned_diagnostics_for_each_rejection()
    {
        foreach (var vector in _corpus.RejectionVectors)
        {
            string[] expected = [.. vector.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")];
            _rejections[vector.Name].ShouldEqual(expected);
        }
    }

    static IEnumerable<ScreenSyntax> Screens(ApplicationSyntax application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Screens);

    static ArrangementContainerSyntax Grid(ApplicationSyntax application) =>
        (ArrangementContainerSyntax)((ArrangementContainerSyntax)((ArrangementContainerSyntax)application.Modules.Single().ScreenTemplates.Single().Arrangement!.Root!).Children.Single()).Children.Last();
}
