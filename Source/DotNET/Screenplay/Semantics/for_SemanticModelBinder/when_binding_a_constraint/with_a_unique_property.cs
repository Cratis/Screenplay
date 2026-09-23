// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_a_unique_property : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                code String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  code = code
              event ProjectRegistered
                projectId Uuid
                code String
              constraint ProjectCodeIsUnique
                unique code on ProjectRegistered
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_name_as_identity() => Constraint.Name.ShouldEqual("ProjectCodeIsUnique");
    [Fact] void should_constrain_property_values() => Constraint.Kind.ShouldEqual(SemanticConstraintKind.UniquePropertyValue);
    [Fact] void should_be_enforced_across_the_event_sequence() => Constraint.Scope.ShouldEqual(SemanticConstraintScope.EventSequence);
    [Fact] void should_target_the_event() => Constraint.Targets.Single().EventContract.ShouldEqual(Slice.Events.Single().Id);
    [Fact] void should_target_the_property() => Constraint.Targets.Single().Properties.ShouldContainOnly([Slice.Events.Single().Properties.Single(_ => _.Name == "code").Id]);
    [Fact] void should_not_be_released() => Constraint.ReleasedBy.ShouldBeEmpty();
    [Fact] void should_compare_values_with_casing() => Constraint.IgnoreCasing.ShouldBeFalse();
    [Fact] void should_use_the_default_message() => Constraint.Message.ShouldBeNull();

    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
    SemanticConstraint Constraint => Slice.Constraints.Single();
}
