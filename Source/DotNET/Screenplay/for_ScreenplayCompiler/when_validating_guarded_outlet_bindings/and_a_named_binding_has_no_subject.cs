// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_guarded_outlet_bindings;

public class and_a_named_binding_has_no_subject : given.a_component_outlet
{
    void Because() => CompileOutlet(named: true);

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_only_the_missing_subject() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(DiagnosticCodes.UnresolvedActionSubject);
    [Fact] void should_locate_the_warning_at_the_attachment() => _result.Diagnostics.Single().Location.Line.ShouldEqual(21);
}
