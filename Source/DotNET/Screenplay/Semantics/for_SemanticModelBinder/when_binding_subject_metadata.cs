// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_subject_metadata : given.a_semantic_binder
{
    [Theory]
    [InlineData("event Changed\n        customerId Uuid MARK")]
    [InlineData("command Change\n        accountId Uuid identifier\n        customerId Uuid\n        produces event Changed\n          customerId Uuid MARK = customerId")]
    void should_report_lineage_without_changing_any_executable_bytes(string body)
    {
        var source = "module M\n  feature F\n    slice StateChange S\n      " + body;
        var unmarked = Bind(source.Replace("MARK", string.Empty, StringComparison.Ordinal));
        var marked = Bind(source.Replace("MARK", "subject", StringComparison.Ordinal));
        unmarked.Success.ShouldBeTrue();
        marked.Success.ShouldBeTrue();
        marked.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax && diagnostic.Message.Contains("subject property", StringComparison.Ordinal)).ShouldBeTrue();
        SemanticModelSerializer.Serialize(marked.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(unmarked.Value!.Model));
    }

    [Fact]
    void should_still_refuse_protected_concept_execution() =>
        Bind("concept Email : String pii\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        customerId Uuid subject\n        email Email").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
}
