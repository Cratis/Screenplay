// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_binding_anonymous_policy_probes : Specification
{
    CompilationResult<SemanticCompilation> _admitted;
    CompilationResult<SemanticCompilation> _gated;

    void Because()
    {
        _admitted = ContractAdmission.Bind(ContractAdmission.AnonymousPolicies);
        _gated = ContractAdmission.Bind(ContractAdmission.AnonymousPolicies.Replace("module M\n", "policy Staff\n  require role \"Staff\"\nmodule M\n  authorize Staff\n", StringComparison.Ordinal));
    }

    [Fact] void should_bind_the_anonymous_policy() => Assert.True(_admitted.Success, string.Join('\n', _admitted.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_the_anonymous_policy_version() => _admitted.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V31);
    [Fact] void should_refuse_an_anonymous_gate_under_another_gate() => _gated.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.AnonymousPolicyUnderEnclosingGate);
}
