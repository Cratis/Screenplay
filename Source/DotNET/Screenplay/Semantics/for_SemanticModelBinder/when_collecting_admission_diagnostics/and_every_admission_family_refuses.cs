// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_collecting_admission_diagnostics;

public class and_every_admission_family_refuses : given.a_semantic_binder
{
    const string Source =
        """
        identity
          department String from claim "department"
        agents
          tokens personal
          command Missing
            scope write
            description "Missing"
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
                  department = $identity.department
              event ProjectRegistered
                department String
            slice StateView View
              readmodel Parent
                child Child[]
              readmodel Child
                value String
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_refuse_the_model() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_produce_a_compiled_value() => _result.Value.ShouldBeNull();
    [Fact] void should_collect_agent_errors_then_member_type_errors_then_identity_errors() => string.Join(' ', _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")).ShouldEqual("PLAY0966@5:3 PLAY0273@20:15 PLAY0268@15:11");
}
