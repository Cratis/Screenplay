// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_member_names_another_read_model : given.a_member
{
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_fail_ordinary_validation() => Result.Success.ShouldBeFalse();
    [Fact] void should_report_one_error() => Result.Diagnostics.Single().Code.ShouldEqual("PLAY0273");
    [Fact] void should_preserve_error_severity() => Result.Diagnostics.Single().Severity.ShouldEqual(Diagnostics.DiagnosticSeverity.Error);
    [Fact] void should_explain_the_value_type_alternative() => Result.Diagnostics.Single().Message.ShouldEqual(Message);
    [Fact] void should_locate_the_member_line() => Result.Diagnostics.Single().Location.Line.ShouldEqual(5);
    [Fact] void should_preserve_the_member_column() => Result.Diagnostics.Single().Location.Column.ShouldEqual(15);
}
