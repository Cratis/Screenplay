// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations.given;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_property_order_without_readmodels : Specification
{
    SemanticBindingOptionsResult _result;
    byte[] _baseline;

    void Because()
    {
        _baseline = SemanticModelSerializer.Serialize(property_order_binding.BindDefault(an_invoice_closing_source.Golden).Value!.Model);
        _result = property_order_binding.Bind(new(PreserveReadModelPropertyOrder: true), an_invoice_closing_source.Golden);
    }

    [Fact] void should_succeed_without_fabricating_metadata() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_old_version() => _result.Compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34);
    [Fact] void should_preserve_every_byte() => SemanticModelSerializer.Serialize(_result.Compilation.Value!.Model).ShouldEqual(_baseline);
}
