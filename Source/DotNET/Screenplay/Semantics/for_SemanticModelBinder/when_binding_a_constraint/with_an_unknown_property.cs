// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_an_unknown_property : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                projectId Uuid
                code String
              event ProjectRenamed
                name String
              constraint ProjectNameIsUnique
                unique name on ProjectRegistered
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_at_the_constraint() => Unknown.Location.Line.ShouldEqual(10);
    [Fact] void should_name_the_property_and_its_event() => Unknown.Message.ShouldEqual("Constraint 'ProjectNameIsUnique' names property 'name', which event 'ProjectRegistered' does not declare.");

    Diagnostic Unknown => _result.Diagnostics.Single(_ => _.Code == DiagnosticCodes.UnknownConstraintProperty);
}
