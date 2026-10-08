// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_binding_admission_probes : Specification
{
    public static TheoryData<string> Probes
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var probe in ContractAdmission.Probes) data.Add(probe.Keyword);

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Probes))]
    void should_agree_with_the_binders_disposition(string keyword) => ContractAdmission.Create([keyword], ScreenplayContract.SupportedVersions()).ShouldNotBeEmpty();
}
