// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_persona_still_references_an_excluded_policy : given.security_documents
{
    void Establish() => Source(Policy + "persona Clerk\n  policy Access\n" + Authorized);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_a_strict_binding_failure() => _normal.Success.ShouldBeFalse();
    [Fact] void should_bind_the_closed_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_only_the_unprotected_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_disclose_the_whole_persona_removal() => _result.Exclusions.Single(exclusion => exclusion.Kind == SemanticExclusionKind.Policy).Reason.ShouldContain("persona 'Clerk' at main.play:3:1");
    [Fact] void should_preserve_the_policy_failure_evidence() => _result.Exclusions.Single(exclusion => exclusion.Kind == SemanticExclusionKind.Policy).Diagnostics.ShouldEqual(_normal.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
}
