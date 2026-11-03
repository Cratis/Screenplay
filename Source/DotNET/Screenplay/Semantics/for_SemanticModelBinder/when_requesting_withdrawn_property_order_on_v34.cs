// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_requesting_withdrawn_property_order_on_v34 : Specification
{
    CompilationResult<SemanticCompilation> _baseline;
    SemanticBindingOptionsResult _result;
    SemanticBindingOptionsResult _disabled;

    void Because()
    {
        var source = an_invoice_closing_source.Golden;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var parsed = new ScreenplayCompiler().Parse(source);
        var documents = SemanticDocumentSet.Create([document], catalog);
        var binder = new SemanticModelBinder(2026);
        _baseline = binder.Bind("Billing", parsed.Value!, documents);
        _result = binder.Bind("Billing", parsed.Value!, documents, new(PreserveReadModelPropertyOrder: true));
        _disabled = binder.Bind("Billing", parsed.Value!, documents, new());
    }

    [Fact] void should_leave_default_binding_successful() => _baseline.Success.ShouldBeTrue();
    [Fact] void should_select_v34_without_the_request() => _baseline.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34);
    [Fact] void should_refuse_the_withdrawn_request() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_v34_as_the_base_version() => _result.Refusal!.BaseVersion.ShouldEqual(SemanticVersion.V34);
    [Fact] void should_not_return_an_executable_to_serialize() => _result.Compilation.Value.ShouldBeNull();
    [Fact] void should_preserve_success_when_the_option_is_disabled() => _disabled.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_default_revision_when_disabled() => _disabled.Compilation.Value!.Model.Revision.ShouldEqual(_baseline.Value!.Model.Revision);
}
