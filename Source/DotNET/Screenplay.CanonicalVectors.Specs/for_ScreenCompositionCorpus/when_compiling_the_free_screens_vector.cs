// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The free screens vector (decision 0165): screens declared at the top level, in a module and in a feature that state their needs
/// without a query, the shape that tightens them, and a slice that adopts them with the data it provides. It compiles without a
/// warning, every syntax kind it names is present, and it survives a print and a syntax JSON round trip.
/// </summary>
public class when_compiling_the_free_screens_vector : Specification
{
    CanonicalTypedScreenSourceCase _case;
    CompilationResult<ApplicationSyntax> _result;
    string _printed;
    CompilationResult<ApplicationSyntax> _reprinted;
    ApplicationSyntax _deserialized;

    void Because()
    {
        _case = ScreenCompositionCorpus.V1.TypedSourceCases.Single(sourceCase => sourceCase.Name == "free-screens-positive");
        _result = new ScreenplayCompiler().Compile(_case.Document.Text);
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _reprinted = new ScreenplayCompiler().Compile(_printed);
        _deserialized = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_result.Value!));
    }

    [Fact] void should_compile_without_a_warning_or_an_error() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_leave_nothing_design_only() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.DesignOnlyScreen).ShouldBeEmpty();
    [Fact] void should_declare_a_screen_at_every_level() => (_result.Value!.Screens.Count(), _result.Value!.Modules.Single().Screens.Count(), _result.Value!.Modules.Single().Features.Single().Screens.Count()).ShouldEqual((1, 1, 1));
    [Fact] void should_adopt_every_free_screen() => _result.Value!.Modules.Single().Features.Single().Slices.Single().UsedScreens.Select(used => used.Screen).ShouldEqual("InvoiceOverview", "InvoiceDigest", "InvoiceDetails");
    [Fact] void should_keep_the_alias_that_tells_two_needs_apart() => _result.Value!.Screens.Single().Directives.OfType<ScreenNeedSyntax>().Select(need => need.Name).ShouldEqual("open", "overdue");
    [Fact] void should_carry_every_expected_syntax_kind() => new KindCollector().Collect(_result.Value!).ShouldContain(_case.ExpectedSyntaxKinds);
    [Fact] void should_print_a_model_that_compiles_again() => _reprinted.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_print_the_same_model_twice() => new ScreenplayPrinter().Print(_reprinted.Value!).ShouldEqual(_printed);
    [Fact] void should_round_trip_through_syntax_json_losslessly() => SyntaxJson.StructurallyEqual(_result.Value!, _deserialized).ShouldBeTrue();

    sealed class KindCollector : ScreenplaySyntaxWalker
    {
        readonly HashSet<string> _kinds = [];

        public override void VisitNode(SyntaxNode node)
        {
            _kinds.Add(node.GetType().Name);
            base.VisitNode(node);
        }

        internal HashSet<string> Collect(ApplicationSyntax application)
        {
            VisitApplication(application);
            return _kinds;
        }
    }
}
