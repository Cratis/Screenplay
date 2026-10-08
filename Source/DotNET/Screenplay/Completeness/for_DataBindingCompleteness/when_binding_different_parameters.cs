// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness;

public class when_binding_different_parameters : given.a_screen
{
    void Establish() => Compile("data R[] via query Q by first\ndata R[] via query Q by second");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.DataBindings]));

    [Fact] void should_report_the_conflict() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ConflictingScreenDataBinding);
}
