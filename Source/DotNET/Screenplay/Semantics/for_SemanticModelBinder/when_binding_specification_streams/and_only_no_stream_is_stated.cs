// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_specification_streams;

public class and_only_no_stream_is_stated : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module M
          feature F
            slice StateView S
              event E
              specification X
                when append E
                then E
                  no stream
        """);

    [Fact] void should_admit_the_unrouted_assertion_without_any_source_declaration() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_event_routes_and_preserve_the_unrouted_assertion()
    {
        _result.Value!.Model.SemanticVersion.ShouldEqual(EventRoutesVersion.Semantic);
        _result.Value.Model.Application.EventSources.ShouldBeEmpty();
        _result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().ThenEvents.Single().Unrouted.ShouldBeTrue();
    }
}
