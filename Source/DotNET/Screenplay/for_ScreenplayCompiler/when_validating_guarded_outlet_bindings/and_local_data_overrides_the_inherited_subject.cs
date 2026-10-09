// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_guarded_outlet_bindings;

public class and_local_data_overrides_the_inherited_subject : given.a_component_outlet
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_use_local_data_for_valid_fields(bool named)
    {
        CompileOutlet(named, screenData: "data Other via query OtherDetails", outletData: "data Item via query ItemDetails");
        _result.Success.ShouldBeTrue();
        _result.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_report_a_field_missing_from_local_data(bool named)
    {
        CompileOutlet(named, screenData: "data Item via query ItemDetails", outletData: "data Other via query OtherDetails");
        _result.Success.ShouldBeTrue();
        _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(DiagnosticCodes.UnknownActionSubjectField);
        _result.Diagnostics.Single().Location.Line.ShouldEqual(named ? 3 : 22);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_report_a_field_missing_from_inherited_data(bool named)
    {
        CompileOutlet(named, screenData: "data Item via query ItemDetails", field: "missing");
        _result.Success.ShouldBeTrue();
        _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(DiagnosticCodes.UnknownActionSubjectField);
    }
}
