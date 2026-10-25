// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_a_slice_publishes_what_another_slice_appends;

public class and_the_command_specification_states_the_publication : given.a_publishing_model
{
    const string Stated =
        """
                then PriceSet
                  price = 59
                then PriceChanged
                  productId = "p1"
                  newPrice = 59
            slice Translate PublishPrice
        """;

    CompilationResult<Syntax.ApplicationSyntax> _syntax;

    void Because() => _syntax = new ScreenplayCompiler().Compile(Model.Replace(
        """
                then PriceSet
                  price = 59
            slice Translate PublishPrice
        """,
        Stated,
        StringComparison.Ordinal));

    [Fact] void should_call_the_other_slices_event_unreachable() =>
        _syntax.Diagnostics.Select(_ => _.Code).ShouldContain(DiagnosticCodes.UnreachableSpecificationOutcome);
}
