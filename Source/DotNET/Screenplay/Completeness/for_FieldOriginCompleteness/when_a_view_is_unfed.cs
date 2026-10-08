// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_a_view_is_unfed : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          readmodel R
            name String
            count Int
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_report_the_declaration_once() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ReadModelFieldWithoutOrigin);
    [Fact] void should_locate_the_read_model() => Findings.Single().Location.Line.ShouldEqual(4);
}
