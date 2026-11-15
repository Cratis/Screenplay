// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
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
            [("Documentation/screenplay/fixtures/operations.play", [typeof(OperationSyntax), typeof(OperationPhaseSyntax), typeof(SpecificationOperationFailureSyntax), typeof(SpecificationOperationSyntax), typeof(SpecificationCompensatedSyntax)])]),
        ("`provide` from a file or code block", 1300,
            [("Documentation/screenplay/fixtures/provide-implementation.play", [typeof(CodeProvideSourceSyntax), typeof(FileProvideSourceSyntax)])]),
        ("`compute`, `entering` bodies, approval `via`, operation-phase `invokes`, `$trigger` values", 1252,
            [("Documentation/screenplay/fixtures/ability-invocations.play", [])])
    ];

    static readonly (Type Node, string Path, string Reason)[] CoveredElsewhere =
    [
        (typeof(QueryProvideSourceSyntax), "Documentation/screenplay/fixtures/provide.play", "Provided values (ESM v53, decision 0161) are shown by the bound provide fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(HostProvideSourceSyntax), "Documentation/screenplay/fixtures/provide.play", "Provided values (ESM v53, decision 0161) are shown by the bound provide fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(ApplicationContextSyntax), "Documentation/screenplay/fixtures/clock-preview.play", "Clock admission (ESM v37, decisions 0106, 0107 and 0132) is shown by the bound clock fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(ContextMemberSyntax), "Documentation/screenplay/fixtures/clock-preview.play", "Clock admission (ESM v37, decisions 0106, 0107 and 0132) is shown by the bound clock fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(TimeZoneSyntax), "Documentation/screenplay/fixtures/clock-preview.play", "Clock admission (ESM v37, decisions 0106, 0107 and 0132) is shown by the bound clock fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(CommandCalendarSyntax), "Documentation/screenplay/fixtures/clock-preview.play", "Clock admission (ESM v37, decisions 0106, 0107 and 0132) is shown by the bound clock fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(SpecificationTimeZoneSyntax), "Documentation/screenplay/fixtures/clock-preview.play", "Clock admission (ESM v37, decisions 0106, 0107 and 0132) is shown by the bound clock fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(ClockOffsetExpressionSyntax), "Documentation/screenplay/fixtures/clock-preview.play", "Clock admission (ESM v37, decisions 0106, 0107 and 0132) is shown by the bound clock fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(DateOfExpressionSyntax), "Documentation/screenplay/fixtures/clock-preview.play", "Clock admission (ESM v37, decisions 0106, 0107 and 0132) is shown by the bound clock fixture, so the TypeScript-conformed Samples/Invoicing keeps its shape."),
        (typeof(UserSyntax), "Source/Screenplay/Compiler/Conformance/users.play", "Development and test users (decision 0141) are shown by the TypeScript-conformed users document; they are tooling metadata, and Samples/Invoicing stays free of them."),
        (typeof(GraphCollectSyntax), "Source/Screenplay/Compiler/Conformance/graph-collects.play", "Graph queries that collect nested rows (ESM v54, decision 0163) are shown by the TypeScript-conformed graph-collects document and by Samples/Organization, which reads its graph as views."),
        (typeof(GraphQuerySyntax), "Source/Screenplay/Compiler/Conformance/graph-queries.play", "Graph queries (ESM v40, decision 0139) are shown by the TypeScript-conformed graph-queries document, as graphs are by graphs.play."),
        (typeof(GraphMatchSyntax), "Source/Screenplay/Compiler/Conformance/graph-queries.play", "Graph queries (ESM v40, decision 0139) are shown by the TypeScript-conformed graph-queries document, as graphs are by graphs.play."),
        (typeof(GraphPatternNodeSyntax), "Source/Screenplay/Compiler/Conformance/graph-queries.play", "Graph queries (ESM v40, decision 0139) are shown by the TypeScript-conformed graph-queries document, as graphs are by graphs.play."),
        (typeof(GraphPatternHopSyntax), "Source/Screenplay/Compiler/Conformance/graph-queries.play", "Graph queries (ESM v40, decision 0139) are shown by the TypeScript-conformed graph-queries document, as graphs are by graphs.play."),
        (typeof(GraphConditionSyntax), "Source/Screenplay/Compiler/Conformance/graph-queries.play", "Graph queries (ESM v40, decision 0139) are shown by the TypeScript-conformed graph-queries document, as graphs are by graphs.play."),
        (typeof(GraphReturnSyntax), "Source/Screenplay/Compiler/Conformance/graph-queries.play", "Graph queries (ESM v40, decision 0139) are shown by the TypeScript-conformed graph-queries document, as graphs are by graphs.play."),
        (typeof(Syntax.Projections.RemoveKeyMappingSyntax), "Source/Screenplay/Compiler/Conformance/projection-parity.play", "A dictionary entry needs a dictionary-typed read-model property, which Screenplay cannot declare (PLAY0962), so a warning-free sample cannot remove one."),
        (typeof(NarrowSyntax), "Source/Screenplay/Compiler/Conformance/narrowed-queries.play", "Row narrowing of a collection query (decision 0154) is shown by the narrowed-queries conformance document; Samples/Invoicing keeps its shape."),
        (typeof(PolicyInputSyntax), "Source/Screenplay/Compiler/Conformance/policy-inputs.play", "Typed policy inputs (decision 0153) are shown by the policy-inputs conformance document; Samples/Invoicing keeps its shape."),
        (typeof(PolicyInputMappingSyntax), "Source/Screenplay/Compiler/Conformance/policy-inputs.play", "Policy input mappings (decision 0153) are shown by the policy-inputs conformance document; Samples/Invoicing keeps its shape."),
        (typeof(StateConditionSyntax), "Source/Screenplay/Compiler/Conformance/state-policies.play", "Policies that decide from recorded state (decision 0151) are shown by the TypeScript-conformed state-policies document; Samples/Invoicing keeps its shape."),
        (typeof(FileImportSyntax), "Samples/Commerce/application.play", "Composition is shown by Commerce, not a single-document application."),
        (typeof(FileConstraintSyntax), "Source/DotNET/Screenplay/for_ScreenplayCompiler/invoicing.play", "Legacy file constraints warn with PLAY0396; with_a_file pins the warning, so they cannot enter warning-free Samples."),
        (typeof(SpecificationGeneratedValueSyntax), "Source/Screenplay/Compiler/Conformance/acme-second-admissions.play", "Pinned generated values need a reaction that invokes a command with a generated identifier; the conformance vector and the Acme Commerce sample show it."),
        (typeof(TemplateAssignmentSyntax), "Documentation/screenplay/fixtures/screen-release-ui.play", "Hierarchical template assignments are covered by the focused round-trip spec until TypeScript conformance admits them into Samples/Invoicing."),
        (typeof(TemplateExposedValueSyntax), "Documentation/screenplay/fixtures/screen-release-ui.play", "Template metadata is covered by the focused round-trip spec until TypeScript conformance admits it into Samples/Invoicing."),
        (typeof(TemplateSlotSyntax), "Documentation/screenplay/fixtures/screen-release-ui.play", "Template metadata is covered by the focused round-trip spec until TypeScript conformance admits it into Samples/Invoicing."),
    ];

    // No production error/trivia SyntaxNode kinds exist today. Never use this list for authoring constructs.
    static readonly Type[] Infrastructure = [];

    readonly List<string> _fixtureFindings = [];
    readonly List<string> _elsewhereFindings = [];
    Type[] _missing;
    ApplicationSyntax _invoicing;
    ApplicationSyntax _exact;
    string[] _documentedPreview;
    ApplicationSyntax _dispatches;
    CommandCalendarSyntax _clockPreviewCalendar;
    CommandCalendarSyntax _documentedClockCalendar;

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
            }
        }

        var clockSource = File.ReadAllText(Path.Combine(root, "Documentation/screenplay/fixtures/clock-preview.play"));
        var clockFixture = compiler.Compile(clockSource);
        _fixtureFindings.AddRange(clockFixture.Diagnostics.Select(diagnostic => $"Documentation/screenplay/fixtures/clock-preview.play: {diagnostic.Code} {diagnostic.Message}"));
        var clockWorkspace = ScreenplayWorkspace.Create("Clock",
            [WorkspaceDocument.Create("clock", PortablePlayPath.Parse("clock.play"), Encoding.UTF8.GetBytes(clockSource))],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Clock")));
        _fixtureFindings.AddRange(clockWorkspace.Compilation.Diagnostics.Select(diagnostic => $"Documentation/screenplay/fixtures/clock-preview.play: must bind, but {diagnostic.Code} {diagnostic.Message}"));
        _clockPreviewCalendar = SyntaxNodes.Under(clockFixture.Value!).OfType<CommandCalendarSyntax>().Single();

        foreach (var bound in new[] { "provide", "availability" })
        {
            var boundPath = $"Documentation/screenplay/fixtures/{bound}.play";
            var boundSource = File.ReadAllText(Path.Combine(root, boundPath));
            _fixtureFindings.AddRange(compiler.Compile(boundSource).Diagnostics.Select(diagnostic => $"{boundPath}: {diagnostic.Code} {diagnostic.Message}"));
            var boundWorkspace = ScreenplayWorkspace.Create("Bound",
                [WorkspaceDocument.Create("bound", PortablePlayPath.Parse("bound.play"), Encoding.UTF8.GetBytes(boundSource))],
                SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Bound")));
            _fixtureFindings.AddRange(boundWorkspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error).Select(diagnostic => $"{boundPath}: must bind, but {diagnostic.Code} {diagnostic.Message}"));
        }

        var calendarExample = File.ReadAllText(Path.Combine(root, "Documentation/screenplay/commands.md"))
            .Split('`').First(example => example.StartsWith("calendar from $context.", StringComparison.Ordinal));
        var calendarApplication = compiler.Parse($"module Clocks\n  feature Scheduling\n    slice StateChange Schedule\n      command ScheduleAppointment\n        {calendarExample}\n").Value!;
        _documentedClockCalendar = SyntaxNodes.Under(calendarApplication).OfType<CommandCalendarSyntax>().Single();

        // A placed slice file has no module or feature of its own; give it the ones a root import would.
        var placed = File.ReadAllLines(Path.Combine(root, "Samples/Commerce/Fulfillment/Tracking/ReceiveCarrierDispatches.play"));
        _dispatches = compiler.Parse("module Fulfillment\n  feature Tracking\n" + string.Join('\n', placed.Select(line => line.Length == 0 ? line : "    " + line))).Value!;

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
        _documentedPreview = [.. section.Split('\n').Where(line => line.StartsWith("| ", StringComparison.Ordinal)).Skip(2).Select(line => line.Trim())];
    }

    [Fact] void should_account_for_every_missing_kind_and_remove_obsolete_exemptions() => Names(_missing).ShouldEqual(Names(Classified));
    [Fact] void should_classify_each_missing_kind_once() => Classified.GroupBy(type => type).Where(group => group.Count() != 1).Select(group => group.Key.Name).ShouldBeEmpty();
    [Fact] void should_keep_each_preview_fixture_clean_present_and_refused_for_its_own_issue() => string.Join('\n', _fixtureFindings).ShouldEqual(string.Empty);
    [Fact]
    void should_use_the_trusted_context_time_zone_member_in_the_clock_preview_and_documented_example()
    {
        _clockPreviewCalendar.ContextMember.ShouldEqual("timeZone");
        _documentedClockCalendar.ContextMember.ShouldEqual("timeZone");
    }

    [Fact] void should_keep_each_elsewhere_exception_at_its_tested_location() => string.Join('\n', _elsewhereFindings).ShouldEqual(string.Empty);
    [Fact]
    void should_show_an_outbound_translation_with_a_public_event_and_an_event_target_in_invoicing() =>
        SyntaxNodes.Under(_invoicing).OfType<SliceSyntax>().Any(slice => slice.Direction == TranslationDirection.Outbound &&
            SyntaxNodes.Under(slice).OfType<EventSyntax>().Any(@event => @event.Visibility == EventVisibility.Public && @event.Origin is null &&
                slice.Projections.Any(projection => projection.ReadModel == @event.Name))).ShouldBeTrue();
    [Fact]
    void should_show_an_inbound_translation_with_an_origin_and_a_source_events_capture_in_commerce() =>
        SyntaxNodes.Under(_dispatches).OfType<SliceSyntax>().Any(slice => slice.Direction == TranslationDirection.Inbound &&
            SyntaxNodes.Under(slice).OfType<EventSyntax>().Any(@event => @event.Origin == "carrier") &&
            slice.Captures.Any(capture => capture.Source is { } source && CaptureEventsSource.IsEvents(source))).ShouldBeTrue();
    [Fact] void should_show_explicit_no_event_assertions_in_invoicing() => SyntaxNodes.Under(_invoicing).OfType<SpecificationSyntax>().Any(specification => specification.ThenNoEvents && specification.WhenAppended is not null).ShouldBeTrue();
    [Fact] void should_show_recovery_redelivery_in_invoicing() => SyntaxNodes.Under(_invoicing).OfType<SpecificationSyntax>().Any(specification => specification.WhenRedelivered is not null && specification.Given.Any() && specification.ThenEvents.Any()).ShouldBeTrue();
    [Fact] void should_opt_into_atomic_reaction_batches_in_invoicing() => _invoicing.ReactionProductionMode.ShouldEqual(ReactionProductionMode.Atomic);
    [Fact] void should_keep_exact_numeric_mode_out_of_invoicing() => _invoicing.SourceOptions.NumericMode.ShouldEqual(NumericMode.Legacy);
    [Fact] void should_demonstrate_exact_numeric_mode_in_its_preview_fixture() => _exact.SourceOptions.NumericMode.ShouldEqual(NumericMode.Exact);
    [Fact] void should_keep_the_documented_preview_table_equal_to_the_enforced_list() => _documentedPreview.ShouldEqual(Preview.Select(preview => $"| {preview.Construct} | #{preview.Issue} | {string.Join(", ", preview.Fixtures.Select(fixture => $"`{fixture.Path}`"))} |"));

    static IEnumerable<Type> Classified => Preview.SelectMany(preview => preview.Fixtures).SelectMany(fixture => fixture.Nodes)
        .Concat(CoveredElsewhere.Select(elsewhere => elsewhere.Node)).Concat(Infrastructure);

    static string Names(IEnumerable<Type> types) => string.Join(", ", types.Select(type => type.Name).Order(StringComparer.Ordinal));
}
