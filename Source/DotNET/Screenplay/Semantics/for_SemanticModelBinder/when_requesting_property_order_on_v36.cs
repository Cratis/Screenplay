// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_property_order_on_v36 : Specification
{
    CompilationResult<SemanticCompilation> _baseline;
    SemanticBindingOptionsResult _result;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", a_lifecycle_source.Source);
        var parsed = new ScreenplayCompiler().Parse(a_lifecycle_source.Source);
        var documents = SemanticDocumentSet.Create([document], catalog);
        var binder = new SemanticModelBinder(2026);
        _baseline = binder.Bind("Billing", parsed.Value!, documents);
        _result = binder.Bind("Billing", parsed.Value!, documents, new(PreserveReadModelPropertyOrder: true));
    }

    [Fact] void should_leave_default_binding_successful() => _baseline.Success.ShouldBeTrue();
    [Fact] void should_select_v36_without_the_request() => _baseline.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_refuse_the_explicit_request() => _result.Success.ShouldBeFalse();
    [Fact] void should_return_the_existing_typed_refusal() => _result.Refusal.ShouldBeOfExactType<PropertyOrderMetadataRefusal>();
    [Fact] void should_report_v36_as_the_base_version() => _result.Refusal!.BaseVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_explain_the_required_base() => _result.Refusal!.Reason.ShouldContain("requires a valid ESM 34.0 base");
    [Fact] void should_not_return_an_executable_to_serialize() => _result.Compilation.Value.ShouldBeNull();
}
