// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_a_provide_takes_its_value_from_code : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Fleet
          feature Quality
            slice StateView Vessels
              provide limit Int
                file Limit.cs
              event VesselRegistered
                vesselId Uuid
        """);

    SemanticCodeProvideSource Source => (SemanticCodeProvideSource)_result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Provides.Single().Source;

    [Fact] void should_admit_it() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v54() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V54);
    [Fact] void should_carry_a_code_source() => Source.Implementation.ShouldNotBeEmpty();
    [Fact] void should_attach_one_provide_value_requirement() => _result.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.ProvideValue);
    [Fact] void should_name_the_provided_value_as_the_member() => _result.ImplementationRequirements.Single().Member.ShouldEqual("limit");
    [Fact] void should_point_the_source_at_the_requirement() => Source.Implementation.ShouldEqual(_result.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_keep_the_file_path() => _result.ImplementationRequirements.Single().File.ShouldEqual("Limit.cs");
}
