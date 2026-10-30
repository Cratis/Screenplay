// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_compiling_an_invoicing_shaped_model : given.security_documents
{
    void Establish()
    {
        var refused = UnnamedVariantProjection.Replace("value = value\n    slice", "value = balance.outstanding\n    slice", StringComparison.Ordinal);
        var safe = AdmittedProtected.Replace("command reads protected\nmodule M\n", string.Empty, StringComparison.Ordinal)
            .Replace("feature F", "feature Safe", StringComparison.Ordinal)
            .Replace("Recorded", "SafeRecorded", StringComparison.Ordinal)
            .Replace("Removed", "SafeRemoved", StringComparison.Ordinal)
            .Replace("View", "SafeView", StringComparison.Ordinal)
            .Replace("StateSafeView", "StateView", StringComparison.Ordinal)
            .Replace("Update", "SafeUpdate", StringComparison.Ordinal)
            .Replace("Inspect", "SafeInspect", StringComparison.Ordinal)
            .Replace("query Get", "query GetSafe", StringComparison.Ordinal);
        Source(refused + safe);
    }

    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_refused_read() => _normal.Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead);
    [Fact] void should_have_an_unresolved_path() => _normal.Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.UnknownReadPath);
    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_only_the_refused_read_closure() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Write", "M/F/Lookup", "M/F/Inspect"]);
    [Fact] void should_keep_the_clean_protected_read() => _result.Compilation.Value.Model.Application.Modules.Single().Features.Single(value => value.Name == "Safe").Slices.SelectMany(value => value.Commands).SelectMany(value => value.Reads).ShouldNotBeEmpty();

    [Fact] void should_leave_no_unprotected_endpoint() => AssertProtectedReadClosure(_result.Compilation.Value.Model);
    [Fact] void should_emit_no_unprotected_endpoint() => AssertProtectedReadClosure(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Compilation.Value.Model)));

    static void AssertProtectedReadClosure(ExecutableSemanticModel model)
    {
        var slices = model.Application.Modules.Single().Features.SelectMany(value => value.Slices).ToArray();
        var views = slices.SelectMany(value => value.ReadModels).ToArray();
        views.Select(value => value.Name).ShouldEqual(["SafeView"]);
        slices.SelectMany(value => value.Commands).SelectMany(value => value.Reads).ShouldEachConformTo(value => value.ReadModel == views.Single().Id);
    }
}
