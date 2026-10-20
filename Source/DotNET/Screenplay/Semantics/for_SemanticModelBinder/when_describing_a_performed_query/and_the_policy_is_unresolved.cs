// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_the_policy_is_unresolved : for_SemanticModelBinder.given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Purchasing
          feature Overview
            slice StateView Figures
              readmodel Figures
                figureId String
                total Int
              query FigureList => Figures[]
                authorize Missing
                performer
                  file Queries/FigureList.cs
        """);

    [Fact] void should_not_hide_the_unresolved_policy() => _result.Success.ShouldBeFalse();
    [Fact] void should_name_the_policy() => _result.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldContain("Authorization policy 'Missing' is unresolved.");
}
