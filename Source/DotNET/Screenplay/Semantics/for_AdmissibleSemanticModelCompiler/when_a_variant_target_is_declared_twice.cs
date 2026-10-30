// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_variant_target_is_declared_twice : given.security_documents
{
    void Establish() => Source(UnnamedVariantProjection.Replace("    slice StateChange Kept", "    slice StateView Duplicate\n      readmodel View\n        id String key\n        value String\n    slice StateChange Kept", StringComparison.Ordinal));
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_duplicate_read_model_error() => _normal.Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.DuplicateReadModel);
    [Fact] void should_refuse_the_partial_model() => _result.Compilation.Success.ShouldBeFalse();
    [Fact] void should_return_no_model() => _result.Compilation.Value.ShouldBeNull();
    [Fact] void should_preserve_the_original_failure() => _result.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
    [Fact] void should_not_report_partial_exclusions() => _result.Exclusions.ShouldBeEmpty();
}
