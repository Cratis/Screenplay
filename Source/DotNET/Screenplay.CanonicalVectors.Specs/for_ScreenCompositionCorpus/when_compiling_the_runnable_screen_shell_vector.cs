// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The runnable application shell vector: the shell constructs together with the queries, projections and screens the
/// generated backend needs, so the published CLI renders and runs it (Cratis/cli#374). Sample data is declared in it and
/// must never reach a generated application.
/// </summary>
public class when_compiling_the_runnable_screen_shell_vector : Specification
{
    CanonicalTypedScreenSourceCase _case;
    CompilationResult<ApplicationSyntax> _shell;

    void Because()
    {
        _case = ScreenCompositionCorpus.V1.TypedSourceCases.Single(sourceCase => sourceCase.Name == "screen-shell-navigation-runnable");
        _shell = new ScreenplayCompiler().Compile(_case.Document.Text);
    }

    [Fact] void should_compile_without_an_error_or_warning() => _shell.Diagnostics.Where(diagnostic => diagnostic.Severity != Diagnostics.DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_not_be_pending() => _case.PendingReason.ShouldBeNull();
    [Fact] void should_extend_the_sidebar_in_one_module_and_override_it_in_another() => _shell.Value!.Modules.SelectMany(module => module.Navigation).Select(region => region.Mode).Distinct().ShouldContain(NavigationRegionMode.Extend, NavigationRegionMode.Override);
    [Fact] void should_declare_a_toolbar_button_on_a_state_change_slice() => _case.Document.Text.ShouldContain("button new title \"New order\" slice Sales.Orders.PlaceOrder");
    [Fact] void should_declare_sample_data_that_must_not_ship() => _shell.Value!.SampleDataPacks.Single().Name.ShouldEqual("Demo");
}
