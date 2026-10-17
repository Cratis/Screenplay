// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads.with_typed_read_path_arguments;

public class and_an_ambiguous_read_path : given.a_protected_read_model
{
    Diagnostic _diagnostic;

    void Because() => _diagnostic = Bind(Source.Replace("reads View by id", "reads View as first by id\n        reads View as second by id", StringComparison.Ordinal)).Diagnostics.First(value => value.Code == DiagnosticCodes.AmbiguousReadPath);

    [Fact] void should_carry_three_arguments() => _diagnostic.Arguments.Count.ShouldEqual(3);
    [Fact] void should_name_the_path() => _diagnostic.Arguments["path"].ShouldEqual(DiagnosticArgument.Text("View.value"));
    [Fact] void should_name_the_read() => _diagnostic.Arguments["read"].ShouldEqual(DiagnosticArgument.Text("View"));
    [Fact] void should_name_the_command() => _diagnostic.Arguments["command"].ShouldEqual(DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]));
    [Fact] void should_keep_the_message_wording() => _diagnostic.Message.ShouldEqual("Read path 'View.value' must use an alias because 'View' is read more than once.");
}
