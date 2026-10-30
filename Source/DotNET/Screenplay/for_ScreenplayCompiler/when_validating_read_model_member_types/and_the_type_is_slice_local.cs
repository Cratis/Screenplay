// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_type_is_slice_local : given.a_member
{
    protected override string SliceDeclarations => "      type Child\n        value String\n";
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_keep_the_unknown_construct_warning() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0029").Severity.ShouldEqual(Diagnostics.DiagnosticSeverity.Warning);
    [Fact] void should_keep_the_warning_at_the_local_header() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0029").Location.Line.ShouldEqual(6);
    [Fact] void should_keep_the_unknown_construct_message() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0029").Message.ShouldEqual("Unknown construct 'type' in slice 'View'");
    [Fact] void should_reject_the_member_type() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0273").Message.ShouldEqual(Message);
    [Fact] void should_report_only_the_warning_and_member_error() => Result.Diagnostics.Count().ShouldEqual(2);
}
