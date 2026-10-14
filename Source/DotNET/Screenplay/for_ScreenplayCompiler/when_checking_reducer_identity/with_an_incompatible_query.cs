// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_an_incompatible_query : given.a_reducer_identity_model
{
    Diagnostic[] _warnings;

    void Because() => _warnings = Warnings(Model);

    [Fact] void should_warn_once() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_use_warning_severity() => _warnings[0].Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_locate_the_query() => _warnings[0].Location.Line.ShouldEqual(14);
    [Fact] void should_name_the_nominal_domains() => _warnings[0].Message.ShouldContain("by 'month' (Month), but reducer 'Fold' observes 'Recorded' with event-source identity ResourceId");
    [Fact] void should_explain_the_fixed_key() => _warnings[0].Message.ShouldContain("Reducer instances are keyed by each event's event-source id");
    [Fact] void should_recommend_projection_keys_and_joins() => _warnings[0].Message.ShouldContain("property key on each relevant 'from', or a join");
    [Fact] void should_not_claim_impossible_runtime_values() => _warnings[0].Message.Contains("never", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_leave_ordinary_compilation_successful() => new ScreenplayCompiler().Compile(Model).Success.ShouldBeTrue();
}
