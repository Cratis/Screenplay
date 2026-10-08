// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_admitting_slice_local_event_example_destinations : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module M
          feature F
            slice StateChange A
              event E
                amount Int
              command First
                id String identifier
                amount Int
                produces E
                  for id
                  amount = amount
              example Fact : E
                amount = 1
                for "first"
            slice StateChange B
              command Second
                id Uuid identifier
                amount Int
                produces E
                  for id
                  amount = amount
        """);

    [Fact] void should_use_the_declarations_local_producer_for_an_unused_example() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
}
