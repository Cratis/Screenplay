// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_a_command_has_no_ui_issuer : given.a_model
{
    void Establish() => Compile("""
        module M
          feature F
            slice StateChange Change
              command C
                value String
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_report_the_command_gap() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.CommandWithoutInputSurface);
    [Fact] void should_locate_the_command() => Findings.Single().Location.Line.ShouldEqual(4);
}
