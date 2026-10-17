// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_typed_concurrency_arguments : given.a_protected_read_model
{
    DiagnosticArguments _arguments;

    void Because() => _arguments = Bind(Source.Replace("reads View by id", "reads View by id\n        concurrency\n          eventSource", StringComparison.Ordinal))
        .Diagnostics.Single(value => value.Code == DiagnosticCodes.DecisionReadsWithConcurrency).Arguments;

    [Fact] void should_name_only_the_command() => _arguments.Count.ShouldEqual(1);
    [Fact] void should_name_the_command() => _arguments["command"].ShouldEqual(DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]));
}
