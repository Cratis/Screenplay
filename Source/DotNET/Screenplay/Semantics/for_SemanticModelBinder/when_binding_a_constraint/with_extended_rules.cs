// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_extended_rules : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                code String
                year Int
              event ProjectImported
                code String
                year Int
              event ProjectReleased
              event ProjectExpired
              constraint ProjectCodeIsUnique
                unique code, year on ProjectRegistered
                unique code, year on ProjectImported
                released by ProjectReleased
                released by ProjectExpired
                ignore casing
                message "$strings.duplicateCode"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_two_targets() => Constraint.Targets.Length.ShouldEqual(2);
    [Fact] void should_have_composite_keys() => Constraint.Targets.All(_ => _.Properties.Length == 2).ShouldBeTrue();
    [Fact] void should_have_two_releases() => Constraint.ReleasedBy.Length.ShouldEqual(2);
    [Fact] void should_ignore_casing() => Constraint.IgnoreCasing.ShouldBeTrue();
    [Fact] void should_keep_the_message_key_verbatim() => Constraint.Message.ShouldEqual("$strings.duplicateCode");

    SemanticConstraint Constraint => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Constraints.Single();
}
