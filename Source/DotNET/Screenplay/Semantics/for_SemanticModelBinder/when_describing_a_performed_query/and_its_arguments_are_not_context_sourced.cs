// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_its_arguments_are_not_context_sourced : given.a_performed_query
{
    [Fact] void should_accept_the_performed_queries() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_refuse_their_arguments() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ContextSourcedPerformedQueryArgument).ShouldBeFalse();
    [Fact] void should_keep_the_caller_supplied_key() => ShapeOf("CurrentFigures").Arguments.Select(argument => argument.Name).ShouldEqual(["invoiceId"]);
    [Fact] void should_keep_the_caller_supplied_key_parts() => ShapeOf("LedgerPage").Arguments.Select(argument => argument.Name).ShouldEqual(["book", "page"]);
    [Fact] void should_preserve_literal_sourced_filter_behavior() => ShapeOf("FiguresInRegion").Arguments.Select(argument => argument.Name).ShouldEqual(["region", "status"]);
}
