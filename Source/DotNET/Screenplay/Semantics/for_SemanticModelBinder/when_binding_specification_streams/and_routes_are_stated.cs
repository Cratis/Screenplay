// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_specification_streams;

public class and_routes_are_stated : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        eventsource Account
          identifier String
          stream Profile
        module M
          feature F
            slice StateView S
              event E
              specification X
                given E
                  for "other"
                  stream Account.Profile
                when append E
                  for "other"
                  stream Account.Profile
                then E
                  no stream
        """);

    [Fact] void should_bind_all_three_route_nodes()
    {
        var specification = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        specification.GivenEvents.Single().Route.ShouldNotBeNull();
        specification.WhenAppended!.Route.ShouldNotBeNull();
        specification.ThenEvents.Single().Unrouted.ShouldBeTrue();
        _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeFalse();
    }
    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
}
