// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_property_order_on_v36 : Specification
{
    CompilationResult<SemanticCompilation> _baseline;
    SemanticBindingOptionsResult _result;

    void Because()
    {
        _baseline = property_order_binding.BindDefault(a_lifecycle_source.Source);
        _result = property_order_binding.Bind(new(PreserveReadModelPropertyOrder: true), a_lifecycle_source.Source);
    }

    [Fact] void should_leave_default_binding_successful() => _baseline.Success.ShouldBeTrue();
    [Fact] void should_select_v36_without_the_request() => _baseline.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_refuse_the_explicit_request() => _result.Success.ShouldBeFalse();
    [Fact] void should_return_the_existing_typed_refusal() => _result.Refusal.ShouldBeOfExactType<PropertyOrderMetadataRefusal>();
    [Fact] void should_report_v36_as_the_base_version() => _result.Refusal!.BaseVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_explain_the_required_base() => _result.Refusal!.Reason.ShouldContain("requires a valid ESM 34.0 base");
    [Fact] void should_not_return_an_executable_to_serialize() => _result.Compilation.Value.ShouldBeNull();
}
