// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_Samples;

/// <summary>
/// Holds the language showcase to every concrete syntax kind, with explicit, tested exceptions.
/// </summary>
public class when_holding_invoicing_to_the_language : Specification
{
    static readonly (string Construct, int Issue, (string Path, Type[] Nodes)[] Fixtures)[] Preview =
    [

        // The larger exact-numbers transport vector intentionally has unresolved declarations and warnings.
        ("`numbers exact`", 285, [("Source/Screenplay/Compiler/Conformance/exact-named-rule-intent.play", [])]),
        ("`system`, `operation`, operation specifications", 301,
            [("Documentation/screenplay/fixtures/operations.play", [typeof(SystemSyntax), typeof(OperationSyntax), typeof(OperationPhaseSyntax), typeof(SpecificationOperationFailureSyntax), typeof(SpecificationOperationSyntax), typeof(SpecificationCompensatedSyntax)])]),
        ("refusals, redelivery, `then no events`", 433,
            [
                ("Source/Screenplay/Compiler/Conformance/reaction-refusals-redelivery.play", [typeof(InvocationRefusalSyntax), typeof(RefusalExpressionSyntax), typeof(SpecificationRedeliverySyntax)]),
                ("Source/Screenplay/Compiler/Conformance/no-events.play", [])
            ])
    ];

    static readonly (string Construct, string Path, Type[] Nodes)[] AdmittedElsewhere =
    [
        ("`eventsource`, `stream`, command routes", "Documentation/screenplay/fixtures/source-streams.play", [typeof(EventSourceSyntax), typeof(EventStreamSyntax), typeof(EventStreamIdPartSyntax), typeof(CommandStreamSyntax)]),
        ("specification `stream`/`streamId`/`no stream`", "Source/Screenplay/Compiler/Conformance/specification-streams.play", [typeof(SpecificationStreamSyntax), typeof(SpecificationNoStreamSyntax)])
    ];

    static readonly (Type Node, string Path, string Reason)[] CoveredElsewhere =
    [
        (typeof(FileImportSyntax), "Samples/Commerce/application.play", "Composition is shown by Commerce, not a single-document application."),
        (typeof(FileConstraintSyntax), "Source/DotNET/Screenplay/for_ScreenplayCompiler/invoicing.play", "Legacy file constraints warn with PLAY0396; with_a_file pins the warning, so they cannot enter warning-free Samples."),
        (typeof(TemplateAssignmentSyntax), "Documentation/screenplay/fixtures/screen-release-ui.play", "Hierarchical template assignments are covered by the focused round-trip spec until TypeScript conformance admits them into Samples/Invoicing."),
        (typeof(TemplateExposedValueSyntax), "Documentation/screenplay/fixtures/screen-release-ui.play", "Template metadata is covered by the focused round-trip spec until TypeScript conformance admits it into Samples/Invoicing."),
        (typeof(TemplateOutletSyntax), "Documentation/screenplay/fixtures/screen-release-ui.play", "Template metadata is covered by the focused round-trip spec until TypeScript conformance admits it into Samples/Invoicing."),
    ];

    // No production error/trivia SyntaxNode kinds exist today. Never use this list for authoring constructs.
    static readonly Type[] Infrastructure = [];

    readonly List<string> _fixtureFindings = [];
    readonly List<string> _elsewhereFindings = [];
    Type[] _missing;
    ApplicationSyntax _invoicing;
    ApplicationSyntax _exact;
    string[] _documentedPreview;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var compiler = new ScreenplayCompiler();
        _invoicing = compiler.Compile(File.ReadAllText(Path.Combine(root, "Samples/Invoicing/invoicing.play"))).Value!;
        var covered = SyntaxNodes.Under(_invoicing).Select(node => node.GetType()).ToHashSet();

        // Inline specifications share the Debug assembly. Exclude their deliberately unknown test doubles,
        // not internal production nodes; every concrete language node is counted regardless of visibility.
        _missing = [.. typeof(SyntaxNode).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(SyntaxNode).IsAssignableFrom(type) &&
                !type.Namespace!.Split('.').Any(segment => segment.StartsWith("for_", StringComparison.Ordinal) || segment.StartsWith("when_", StringComparison.Ordinal) || segment == "given") &&
                !covered.Contains(type))];

        foreach (var preview in Preview)
        {
            foreach (var fixture in preview.Fixtures)
            {
                var source = File.ReadAllText(Path.Combine(root, fixture.Path));
                var compilation = compiler.Compile(source);
                _fixtureFindings.AddRange(compilation.Diagnostics.Select(diagnostic => $"{fixture.Path}: {diagnostic.Code} {diagnostic.Message}"));
                var application = compilation.Value!;
                var types = SyntaxNodes.Under(application).Select(node => node.GetType()).ToHashSet();
                _fixtureFindings.AddRange(fixture.Nodes.Where(type => !types.Contains(type)).Select(type => $"{fixture.Path}: missing {type.Name}"));
                var workspace = ScreenplayWorkspace.Create("Preview",
                    [WorkspaceDocument.Create("preview", PortablePlayPath.Parse("preview.play"), Encoding.UTF8.GetBytes(source))],
                    SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Preview")));
                if (workspace.Compilation.Success || !workspace.Compilation.Diagnostics.Any(diagnostic =>
                    diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains($"(#{preview.Issue})", StringComparison.Ordinal)))
                {
                    _fixtureFindings.Add($"{fixture.Path}: must refuse binding with PLAY0268 naming (#{preview.Issue})");
                }

                if (preview.Issue == 285) _exact = application;
                if (fixture.Path.EndsWith("no-events.play", StringComparison.Ordinal) && !SyntaxNodes.Under(application).OfType<SpecificationSyntax>().Any(specification => specification.ThenNoEvents))
                {
                    _fixtureFindings.Add($"{fixture.Path}: missing explicit then no events");
                }
            }
        }

        foreach (var admitted in AdmittedElsewhere)
        {
            var source = File.ReadAllText(Path.Combine(root, admitted.Path));
            var application = compiler.Compile(source);
            _elsewhereFindings.AddRange(application.Diagnostics.Select(diagnostic => $"{admitted.Path}: {diagnostic.Code} {diagnostic.Message}"));
            var types = SyntaxNodes.Under(application.Value!).Select(node => node.GetType()).ToHashSet();
            _elsewhereFindings.AddRange(admitted.Nodes.Where(type => !types.Contains(type)).Select(type => $"{admitted.Path}: missing {type.Name}"));
            var workspace = ScreenplayWorkspace.Create("Admitted",
                [WorkspaceDocument.Create("admitted", PortablePlayPath.Parse("admitted.play"), Encoding.UTF8.GetBytes(source))],
                SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Admitted")));
            if (admitted.Path.EndsWith("specification-streams.play", StringComparison.Ordinal))
            {
                // This compiler conformance fixture deliberately supplies partial event payloads.
                // Complete executable routes are pinned by when_binding_specification_streams.
                if (workspace.Compilation.Success || workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code != DiagnosticCodes.MissingSpecificationProperty))
                {
                    _elsewhereFindings.Add($"{admitted.Path}: only incomplete payloads, not route admission, may block binding: {string.Join(';', workspace.Compilation.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message))}");
                }
            }
            else if (!workspace.Compilation.Success || workspace.Compilation.Value!.Model.SemanticVersion != EventRoutesVersion.Semantic)
            {
                _elsewhereFindings.Add($"{admitted.Path}: must bind at the event routes version");
            }
        }

        foreach (var elsewhere in CoveredElsewhere)
        {
            var application = compiler.Parse(File.ReadAllText(Path.Combine(root, elsewhere.Path))).Value!;
            if (!SyntaxNodes.Under(application).Any(node => node.GetType() == elsewhere.Node))
            {
                _elsewhereFindings.Add($"{elsewhere.Path}: missing {elsewhere.Node.Name} ({elsewhere.Reason})");
            }
        }

        var section = File.ReadAllText(Path.Combine(root, ".cratis/ai/rules/project/samples.md"))
            .Split("### Preview constructs", StringSplitOptions.None)[1].Split("###", StringSplitOptions.None)[0];
        // The project table and Invoicing migration are part of the batch claim, outside this integration's ownership.
        // Admitted fixtures are checked above, never held to a preview refusal while the table catches up.
        _documentedPreview = [.. section.Split('\n').Where(line => line.StartsWith("| ", StringComparison.Ordinal)).Skip(2).Select(line => line.Trim())
            .Where(line => !AdmittedElsewhere.Any(admitted => line.StartsWith($"| {admitted.Construct} |", StringComparison.Ordinal)))];
    }

    [Fact] void should_account_for_every_missing_kind_and_remove_obsolete_exemptions() => Names(_missing).ShouldEqual(Names(Classified));
    [Fact] void should_classify_each_missing_kind_once() => Classified.GroupBy(type => type).Where(group => group.Count() != 1).Select(group => group.Key.Name).ShouldBeEmpty();
    [Fact] void should_keep_each_preview_fixture_clean_present_and_refused_for_its_own_issue() => string.Join('\n', _fixtureFindings).ShouldEqual(string.Empty);
    [Fact] void should_keep_each_elsewhere_exception_at_its_tested_location() => string.Join('\n', _elsewhereFindings).ShouldEqual(string.Empty);
    [Fact] void should_keep_explicit_no_event_assertions_out_of_invoicing() => SyntaxNodes.Under(_invoicing).OfType<SpecificationSyntax>().Any(specification => specification.ThenNoEvents).ShouldBeFalse();
    [Fact] void should_keep_exact_numeric_mode_out_of_invoicing() => _invoicing.SourceOptions.NumericMode.ShouldEqual(NumericMode.Legacy);
    [Fact] void should_demonstrate_exact_numeric_mode_in_its_preview_fixture() => _exact.SourceOptions.NumericMode.ShouldEqual(NumericMode.Exact);
    [Fact] void should_keep_the_documented_preview_table_equal_to_the_enforced_list() => _documentedPreview.ShouldEqual(Preview.Select(preview => $"| {preview.Construct} | #{preview.Issue} | {string.Join(", ", preview.Fixtures.Select(fixture => $"`{fixture.Path}`"))} |"));

    static IEnumerable<Type> Classified => Preview.SelectMany(preview => preview.Fixtures).SelectMany(fixture => fixture.Nodes)
        .Concat(AdmittedElsewhere.SelectMany(admitted => admitted.Nodes)).Concat(CoveredElsewhere.Select(elsewhere => elsewhere.Node)).Concat(Infrastructure);

    static string Names(IEnumerable<Type> types) => string.Join(", ", types.Select(type => type.Name).Order(StringComparer.Ordinal));
}
