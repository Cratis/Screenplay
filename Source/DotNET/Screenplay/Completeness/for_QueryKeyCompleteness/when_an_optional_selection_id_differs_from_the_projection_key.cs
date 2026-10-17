// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_an_optional_selection_id_differs_from_the_projection_key : given.a_query
{
    void Establish()
    {
        const string source = """
            concept SelectionId : Uuid
            module Selections
              feature Selecting
                slice StateView Selection
                  event SelectionMade
                    sourceId SelectionId
                  readmodel SelectionView
                    selectionId SelectionId
                  projection Selections => SelectionView
                    from SelectionMade
                      key sourceId
                      selectionId = sourceId
                  query GetSelection => SelectionView optional
                    by selectionId SelectionId optional
            """;
        Compilation = new ScreenplayCompiler().Compile(source);
        Compilation.Diagnostics.ShouldBeEmpty();
        Compilation.Success.ShouldBeTrue();
    }

    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_the_optional_identity_parameter() => Findings.ShouldBeEmpty();
}
