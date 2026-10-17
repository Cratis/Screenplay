// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads.with_typed_refusal_arguments;

public class and_a_read_alias : given.a_protected_read_model
{
    Diagnostic _diagnostic;

    void Because() => _diagnostic = Bind(Source.Replace("reads View by id", "reads View as current by id", StringComparison.Ordinal).Replace("id String", "id Int", StringComparison.Ordinal).Replace("View.value", "current.value", StringComparison.Ordinal))
        .Diagnostics.Single(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead);

    [Fact] void should_name_the_alias_as_the_read() => _diagnostic.Arguments["read"].ShouldEqual(DiagnosticArgument.Text("current"));
    [Fact] void should_still_name_the_read_model() => _diagnostic.Arguments["readModel"].ShouldEqual(DiagnosticArgument.Declaration("readmodel", ["M", "F", "Lookup", "View"]));
}
