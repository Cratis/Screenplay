// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_an_action_has_no_input_surface : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
            slice StateView View
              screen S
                action C
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_report_only_the_action_gap() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ActionWithoutInputSurface);
    [Fact] void should_locate_the_action() => Findings.Single().Location.Line.ShouldEqual(8);
}
