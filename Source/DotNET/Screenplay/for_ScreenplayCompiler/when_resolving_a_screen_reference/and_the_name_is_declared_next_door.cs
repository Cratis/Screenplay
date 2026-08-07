// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_resolving_a_screen_reference;

public class and_the_name_is_declared_next_door : given.a_compiler
{
    // Two slices in one feature each declaring 'All' - the shape a generated document always has, because
    // query names come from C# method names and are unique only per read model.
    const string Source =
        """
        module Invoicing
          feature Preparation
            slice StateView Queue
              query All => QueueReadModel

              screen QueueScreen
                data QueueReadModel[] via query All

            slice StateView Deviations
              query All => DeviationReadModel

              screen DeviationScreen
                data DeviationReadModel[] via query All
                data QueueReadModel[] via query Queue.All
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    // Each screen gets its own slice's 'All' - the innermost match wins, so a slice keeps its vocabulary
    // and the name declared next door does not silently take over.
    [Fact] void should_resolve_each_bare_name_in_its_own_slice() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
}
