// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_persona_still_references_an_excluded_policy : given.security_documents
{
    void Establish() => Source(Policy + "persona Clerk\n  policy Access\n" + Authorized);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_strict_binding_failure() => _normal.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_partial_model() => _result.Compilation.Success.ShouldBeFalse();
    [Fact] void should_return_no_model() => _result.Compilation.Value.ShouldBeNull();
    [Fact] void should_preserve_the_original_failure() => _result.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
    [Fact] void should_not_report_partial_exclusions() => _result.Exclusions.ShouldBeEmpty();
}
