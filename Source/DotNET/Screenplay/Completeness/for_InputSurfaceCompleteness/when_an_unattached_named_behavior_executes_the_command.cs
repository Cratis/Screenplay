// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_an_unattached_named_behavior_executes_the_command : given.a_model
{
    void Establish() => Compile("""
        behavior Reusable
          on click
            execute M.F.Change.C
        module M
          feature F
            slice StateChange Change
              command C
                value String
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_not_count_the_unattached_declaration_as_an_issuer() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.CommandWithoutInputSurface);
}
