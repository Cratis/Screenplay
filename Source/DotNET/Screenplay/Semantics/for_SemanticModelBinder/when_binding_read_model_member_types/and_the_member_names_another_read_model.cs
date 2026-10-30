// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_the_member_names_another_read_model : given.a_member
{
    void Because() => BindParsedSource();

    [Fact] void should_fail_without_a_semantic_model() => Binding.Success.ShouldBeFalse();
    [Fact] void should_report_one_native_error() => Binding.Diagnostics.Single().Code.ShouldEqual("PLAY0273");
    [Fact] void should_preserve_error_severity() => Binding.Diagnostics.Single().Severity.ShouldEqual(Diagnostics.DiagnosticSeverity.Error);
    [Fact] void should_match_the_compiler_message() => Binding.Diagnostics.Single().Message.ShouldEqual(Message);
    [Fact] void should_report_the_real_file() => Binding.Diagnostics.Single().Location.Path.ShouldEqual("model.play");
    [Fact] void should_report_the_member_line() => Binding.Diagnostics.Single().Location.Line.ShouldEqual(5);
    [Fact] void should_report_the_member_column() => Binding.Diagnostics.Single().Location.Column.ShouldEqual(15);
}
