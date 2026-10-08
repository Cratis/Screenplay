// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness;

public class when_binding_different_cardinalities : given.a_screen
{
    void Establish() => Compile("data R[] via query Q\ndata R via query One");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.DataBindings]));

    [Fact] void should_report_the_conflict() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ConflictingScreenDataBinding);
    [Fact] void should_locate_the_second_binding() => Findings.Single().Location.Line.ShouldEqual(14);
    [Fact] void should_report_a_warning() => Findings.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
