// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_binding_command_date_comparison_probes : Specification
{
    CompilationResult<SemanticCompilation> _admitted;
    CompilationResult<SemanticCompilation> _refused;

    void Because()
    {
        _admitted = ContractAdmission.Bind(ContractAdmission.CommandDateComparisons);
        _refused = ContractAdmission.Bind(ContractAdmission.CommandDateComparisons.Replace("end >= start", "end >= today", StringComparison.Ordinal));
    }

    [Fact] void should_bind_the_chronological_command_positions() => Assert.True(_admitted.Success, string.Join('\n', _admitted.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_the_date_comparison_version() => _admitted.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    [Fact] void should_keep_the_clock_operand_refused() => _refused.Success.ShouldBeFalse();
    [Fact] void should_report_the_admission_refusal() => _refused.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    [Fact] void should_not_validate_a_literal_in_the_refused_comparison() => _refused.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
}
