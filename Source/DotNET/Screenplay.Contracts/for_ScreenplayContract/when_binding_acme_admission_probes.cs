// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_binding_acme_admission_probes : Specification
{
    CompilationResult<SemanticCompilation> _admitted;
    CompilationResult<SemanticCompilation> _tooManyPlaces;
    CompilationResult<SemanticCompilation> _differentPrimitive;

    void Because()
    {
        _admitted = ContractAdmission.Bind(ContractAdmission.AcmeAdmissions);
        _tooManyPlaces = ContractAdmission.Bind(ContractAdmission.AcmeAdmissions.Replace("decimals <= 2", "decimals <= 29", StringComparison.Ordinal));
        _differentPrimitive = ContractAdmission.Bind(ContractAdmission.AcmeAdmissions.Replace("concept MovementId : Uuid", "concept MovementId : String", StringComparison.Ordinal));
    }

    [Fact] void should_bind_the_admissions() => Assert.True(_admitted.Success, string.Join('\n', _admitted.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_the_acme_admission_version() => _admitted.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V29);
    [Fact] void should_refuse_more_places_than_a_decimal_holds() => _tooManyPlaces.Success.ShouldBeFalse();
    [Fact] void should_refuse_a_concept_over_another_primitive() => _differentPrimitive.Success.ShouldBeFalse();
}
