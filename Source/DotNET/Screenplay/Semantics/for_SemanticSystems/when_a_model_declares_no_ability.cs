// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticSystems;

public class when_a_model_declares_no_ability : Specification
{
    ExecutableSemanticModel _model;

    void Because() => _model = canonical_serialization_golden_vectors.CreateSemanticModelV49();

    [Fact] void should_keep_its_version() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V49);
    [Fact] void should_carry_no_systems() => _model.Application.Systems.IsDefaultOrEmpty.ShouldBeTrue();
    [Fact] void should_not_write_the_systems_member() => System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model)).ShouldNotContain("\"systems\"");
}
#endif
