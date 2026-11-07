// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_screenplay_5_outlets : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          category Shell
          outlet details
          content

        module Sales
          contribute to Navigation
            navigate to Browse
            label "Invoices"
            destination outlet details

          feature Invoices
            slice StateView Browse
              screen Browse
                component App.Card card
                  outlet detail
                    title "Detail"
                navigate to Browse
                  outlet details
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_template_outlet() => Lines(DiagnosticCodes.RemovedTemplateOutlet).ShouldContainOnly(3);
    [Fact] void should_report_the_component_outlet() => Lines(DiagnosticCodes.RemovedComponentOutlet).ShouldContainOnly(16);
    [Fact] void should_report_the_navigation_outlet() => Lines(DiagnosticCodes.RemovedNavigationOutlet).ShouldContainOnly(19);
    [Fact] void should_report_the_destination_outlet() => Lines(DiagnosticCodes.RemovedDestinationOutlet).ShouldContainOnly(10);
    [Fact] void should_report_errors() => Renamed.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_name_the_migration_in_every_message() => Renamed.All(diagnostic => diagnostic.Message.Contains("screenplay migrate --from 5 --to 6", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_replacement_for_a_component() => Message(DiagnosticCodes.RemovedComponentOutlet).ShouldContain("'outlet detail' in a component is written 'slot detail'");
    [Fact] void should_name_the_replacement_for_a_destination() => Message(DiagnosticCodes.RemovedDestinationOutlet).ShouldContain("'destination outlet details' is written 'destination slot details'");
    [Fact] void should_name_the_replacement_for_a_layout() => Message(DiagnosticCodes.RemovedTemplateOutlet).ShouldContain("'outlet details' in a layout is written 'slot details'");
    [Fact] void should_still_read_the_template_slot() => _result.Value!.Layouts!.Single().RecursiveSlots.Single().Name.ShouldEqual("details");
    [Fact] void should_still_read_the_component_slot() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenComponentSyntax>().Single().Slots.Single().Name.ShouldEqual("detail");
    [Fact] void should_still_read_the_navigation_slot() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenNavigateSyntax>().Single().Slot.ShouldEqual("details");
    [Fact] void should_not_report_the_slots_as_unknown() => _result.Diagnostics.Where(diagnostic => _unknownSlots.Contains(diagnostic.Code)).ShouldBeEmpty();

    static readonly HashSet<string> _unknownSlots = new([DiagnosticCodes.UnknownNavigationSlot, DiagnosticCodes.UnknownNavigationDestination], StringComparer.Ordinal);
    static readonly HashSet<string> _renamedCodes = new([DiagnosticCodes.RemovedTemplateOutlet, DiagnosticCodes.RemovedComponentOutlet, DiagnosticCodes.RemovedNavigationOutlet, DiagnosticCodes.RemovedDestinationOutlet], StringComparer.Ordinal);

    IEnumerable<Diagnostic> Renamed => _result.Diagnostics.Where(diagnostic => _renamedCodes.Contains(diagnostic.Code));

    IEnumerable<int> Lines(string code) => _result.Diagnostics.Where(diagnostic => diagnostic.Code == code).Select(diagnostic => diagnostic.Location.Line);

    string Message(string code) => _result.Diagnostics.Single(diagnostic => diagnostic.Code == code).Message;
}
