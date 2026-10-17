// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads.with_typed_refusal_arguments;

public class and_no_removal : given.a_protected_read_model
{
    Diagnostic _diagnostic;

    void Because() => _diagnostic = Bind(Source.Replace("        remove with Removed\n", string.Empty, StringComparison.Ordinal))
        .Diagnostics.Single(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead);

    [Fact] void should_carry_four_arguments() => _diagnostic.Arguments.Count.ShouldEqual(4);
    [Fact] void should_name_the_command() => _diagnostic.Arguments["command"].ShouldEqual(DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]));
    [Fact] void should_name_the_read() => _diagnostic.Arguments["read"].ShouldEqual(DiagnosticArgument.Text("View"));
    [Fact] void should_name_the_read_model() => _diagnostic.Arguments["readModel"].ShouldEqual(DiagnosticArgument.Declaration("readmodel", ["M", "F", "Lookup", "View"]));
    [Fact] void should_publish_the_reason_keyword() => _diagnostic.Arguments["reason"].ShouldEqual(DiagnosticArgument.Keyword("no-removal"));
    [Fact] void should_keep_the_message_wording() => _diagnostic.Message.ShouldEqual("Read 'View' cannot be protected: NoRemoval.");
}
