// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_binding_command_uuid_comparison_probes : Specification
{
    CompilationResult<SemanticCompilation> _admitted;
    CompilationResult<SemanticCompilation> _ordering;
    CompilationResult<SemanticCompilation> _differentConcepts;
    CompilationResult<SemanticCompilation> _literal;

    void Because()
    {
        _admitted = ContractAdmission.Bind(ContractAdmission.CommandUuidComparisons);
        _ordering = ContractAdmission.Bind(ContractAdmission.CommandUuidComparisons.Replace("approvedBy != requestedBy", "approvedBy < requestedBy", StringComparison.Ordinal));
        _differentConcepts = ContractAdmission.Bind("concept PlantId : Uuid\n" + ContractAdmission.CommandUuidComparisons.Replace("raw Uuid", "raw PlantId", StringComparison.Ordinal).Replace("requestedBy == raw", "requestedBy == raw and approvedBy != raw", StringComparison.Ordinal));
        _literal = ContractAdmission.Bind(ContractAdmission.CommandUuidComparisons.Replace("3f2504e0-4f89-41d3-9a0c-0305e82c3301", "3F2504E0-4F89-41D3-9A0C-0305E82C3301", StringComparison.Ordinal));
    }

    [Fact] void should_bind_the_uuid_command_positions() => Assert.True(_admitted.Success, string.Join('\n', _admitted.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_the_uuid_comparison_version() => _admitted.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
    [Fact] void should_keep_ordering_refused() => _ordering.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    [Fact] void should_refuse_two_different_concepts() => _differentConcepts.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    [Fact] void should_refuse_a_non_canonical_literal() => _literal.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeTrue();
    [Fact] void should_not_validate_a_literal_in_the_refused_ordering() => _ordering.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
}
