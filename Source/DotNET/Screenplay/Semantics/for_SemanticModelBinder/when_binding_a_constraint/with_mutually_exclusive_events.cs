// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_mutually_exclusive_events : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
              event ProjectImported
              event ProjectReleased
              constraint OnlyOneProject
                unique event ProjectRegistered
                unique event ProjectImported
                released by ProjectReleased
                message "Already registered"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_two_targets() => Constraint.Targets.Length.ShouldEqual(2);
    [Fact] void should_have_a_release() => Constraint.ReleasedBy.Length.ShouldEqual(1);
    [Fact] void should_keep_the_message() => Constraint.Message.ShouldEqual("Already registered");

    SemanticConstraint Constraint => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Constraints.Single();
}
