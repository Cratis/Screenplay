// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_binding_acme_second_admission_probes : Specification
{
    CompilationResult<SemanticCompilation> _admitted;
    CompilationResult<SemanticCompilation> _unpinnable;

    void Because()
    {
        _admitted = ContractAdmission.Bind(ContractAdmission.AcmeSecondAdmissions);
        _unpinnable = ContractAdmission.Bind(ContractAdmission.AcmeSecondAdmissions.Replace("Create.shipmentId", "Create.orderId", StringComparison.Ordinal));
    }

    [Fact] void should_bind_the_admissions() => Assert.True(_admitted.Success, string.Join('\n', _admitted.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_the_second_acme_admission_version() => _admitted.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V30);
    [Fact] void should_refuse_to_pin_a_property_that_is_not_generated() => _unpinnable.Success.ShouldBeFalse();
}
