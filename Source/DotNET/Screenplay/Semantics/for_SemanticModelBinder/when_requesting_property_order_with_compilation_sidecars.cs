// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_property_order_with_compilation_sidecars : Specification
{
    const string Authentication = """
        authentication
          provider EntraId

        """;

    CompilationResult<SemanticCompilation> _baseline;
    CompilationResult<SemanticCompilation> _ordered;

    void Because()
    {
        var source = Authentication + property_order_binding.Source;
        _baseline = property_order_binding.BindDefault(source);
        _ordered = property_order_binding.Bind(new(PreserveReadModelPropertyOrder: true), source).Compilation;
    }

    [Fact] void should_select_the_minor() => _ordered.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34_1);
    [Fact] void should_have_environment_requirements_to_keep() => _baseline.Value!.Environment.Keys.ShouldNotBeEmpty();
    [Fact] void should_keep_the_environment_keys() => _ordered.Value!.Environment.Keys.ShouldEqual(_baseline.Value!.Environment.Keys);
    [Fact] void should_keep_the_environment_secrets() => _ordered.Value!.Environment.Secrets.ShouldEqual(_baseline.Value!.Environment.Secrets);
    [Fact] void should_have_typed_contexts_to_restamp() => _ordered.TypedContextDescriptors.ShouldNotBeEmpty();
    [Fact] void should_stamp_typed_contexts_with_the_ordered_model_revision() =>
        _ordered.TypedContextDescriptors.All(value => value.ModelRevision == _ordered.Value!.Model.Revision).ShouldBeTrue();
    [Fact] void should_not_keep_the_superseded_model_revision() =>
        _ordered.TypedContextDescriptors.Any(value => value.ModelRevision == _baseline.Value!.Model.Revision).ShouldBeFalse();
}
