// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// Taking a value from an occurrence is a claim about what the occurrence carries, and the document knows
/// what an event and a declared trigger carry - so it is a claim that can be wrong and worth reporting.
/// </summary>
public class when_a_reaction_takes_a_value_its_trigger_lacks : given.a_compiler
{
    const string Source =
        """
        trigger DirectoryChanged
          entry

        module Sales
          feature Orders
            slice StateChange PlaceOrder
              event OrderPlaced
                order String

            slice Automation HandleOrder
              reaction HandleOrder
                when OrderPlaced
                  order
                  customer

            slice Automation SyncDirectory
              reaction SyncDirectory
                when DirectoryChanged
                  entry
                  removedAt
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_warnings() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_one_per_value_the_occurrence_lacks() => _result.Diagnostics.Count().ShouldEqual(2);
    [Fact] void should_report_them_under_the_unknown_value_code() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly(DiagnosticCodes.UnknownTriggerData, DiagnosticCodes.UnknownTriggerData);
    [Fact] void should_name_the_value_the_event_does_not_carry() => _result.Diagnostics.First().Message.ShouldEqual("'OrderPlaced' carries no 'customer' - a reaction can only take values the occurrence provides");
    [Fact] void should_name_the_value_the_trigger_does_not_carry() => _result.Diagnostics.Last().Message.ShouldEqual("'DirectoryChanged' carries no 'removedAt' - a reaction can only take values the occurrence provides");
    [Fact] void should_report_them_as_warnings() => _result.Diagnostics.Select(_ => _.Severity).ShouldContainOnly(DiagnosticSeverity.Warning, DiagnosticSeverity.Warning);
}
