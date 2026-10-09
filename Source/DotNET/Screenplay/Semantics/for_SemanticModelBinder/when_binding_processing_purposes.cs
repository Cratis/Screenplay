// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_processing_purposes : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _plain;
    CompilationResult<SemanticCompilation> _purposes;

    void Because()
    {
        _plain = Bind("module M\n  feature F\n    slice StateChange S\n      command C");
        _purposes = Bind("purpose Billing\n  basis contract\nmodule M\n  purpose Billing\n  feature F\n    slice StateChange S\n      purpose Billing\n      command C");
    }

    [Fact] void should_bind_report_only_metadata() => _purposes.Success.ShouldBeTrue();
    [Fact] void should_report_every_purpose_disposition() => _purposes.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax).ShouldEqual(3);
    [Fact] void should_preserve_executable_bytes() => _purposes.Value!.Model.Revision.ShouldEqual(_plain.Value!.Model.Revision);
    [Fact] void should_preserve_the_model_version() => _purposes.Value!.Model.SemanticVersion.ShouldEqual(_plain.Value!.Model.SemanticVersion);
}
