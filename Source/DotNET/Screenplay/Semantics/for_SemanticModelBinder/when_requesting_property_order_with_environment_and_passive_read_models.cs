// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_property_order_with_environment_and_passive_read_models : Specification
{
    const string Environment = """
        persona Owner

        authentication
          provider EntraId

        onboarding
          invite tenant as Owner

        """;

    SemanticCompilation _baseline;
    SemanticCompilation _ordered;

    void Because()
    {
        var source = Environment + property_order_binding.Source.Replace("    readmodel Entry", "    readmodel Entry\n        passive", StringComparison.Ordinal);
        _baseline = property_order_binding.BindDefault(source).Value!;
        _ordered = property_order_binding.Bind(new(PreserveReadModelPropertyOrder: true), source).Compilation.Value!;
    }

    [Fact] void should_promote_to_the_minor() => _ordered.Model.SemanticVersion.ShouldEqual(SemanticVersion.V37_1);
    [Fact] void should_have_environment_requirements_to_preserve() => _baseline.Environment.Keys.ShouldNotBeEmpty();
    [Fact] void should_have_passive_read_models_to_preserve() => _baseline.PassiveReadModels.ShouldNotBeEmpty();
    [Fact] void should_preserve_the_environment_requirements() => _ordered.Environment.Keys.SequenceEqual(_baseline.Environment.Keys).ShouldBeTrue();
    [Fact] void should_preserve_the_passive_read_models() => _ordered.PassiveReadModels.ShouldEqual(_baseline.PassiveReadModels);
}
