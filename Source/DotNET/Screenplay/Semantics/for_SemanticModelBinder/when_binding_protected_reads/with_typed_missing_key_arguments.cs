// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_typed_missing_key_arguments : given.a_protected_read_model
{
    DiagnosticArguments _arguments;

    void Because() => _arguments = Bind(Source.Replace("reads View by id", "reads View as current", StringComparison.Ordinal).Replace("id String identifier", "id String", StringComparison.Ordinal).Replace("View.value", "current.value", StringComparison.Ordinal))
        .Diagnostics.Single(value => value.Code == DiagnosticCodes.DecisionReadWithoutIdentifier).Arguments;

    [Fact] void should_name_only_the_command_and_read() => _arguments.Count.ShouldEqual(2);
    [Fact] void should_name_the_command() => _arguments["command"].ShouldEqual(DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]));
    [Fact] void should_name_the_alias_as_the_read() => _arguments["read"].ShouldEqual(DiagnosticArgument.Text("current"));
}
