// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_property_order_below_v37 : Specification
{
    static string Source => property_order_binding.LifecycleOnlySource;
    CompilationResult<SemanticCompilation> _baseline;
    SemanticBindingOptionsResult _result;

    void Because()
    {
        _baseline = property_order_binding.BindDefault(Source);
        _result = property_order_binding.Bind(new(PreserveReadModelPropertyOrder: true), Source);
    }

    [Fact] void should_leave_default_binding_successful() => _baseline.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_lifecycle_only_version() => _baseline.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_refuse_the_explicit_request() => _result.Success.ShouldBeFalse();
    [Fact] void should_return_a_typed_refusal() => _result.Refusal.ShouldBeOfExactType<PropertyOrderMetadataRefusal>();
    [Fact] void should_report_the_unmodified_base_version() => _result.Refusal!.BaseVersion.ShouldEqual(_baseline.Value!.Model.SemanticVersion);
    [Fact] void should_explain_the_required_base() => _result.Refusal!.Reason.ShouldContain("requires a valid ESM 37.0 base");
    [Fact] void should_not_silently_return_an_executable_without_requested_metadata() => _result.Compilation.Value.ShouldBeNull();

    [Fact]
    void should_preserve_bytes_when_no_metadata_is_requested() =>
        SemanticModelSerializer.Serialize(property_order_binding.Bind(new(), Source).Compilation.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(_baseline.Value!.Model));
}
