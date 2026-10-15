// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_ui_profile_selecting_sample_data : given.a_compiler
{
    const string Source =
        """
        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Accounts
                name String
              readmodel Tickets
                subject String

        sample data NordicDemo
          locale nb-NO
          shape Customer
            row
              name = "Fjordline AS"

        sample data HelpdeskDemo
          shape Tickets
            row
              subject = "Reset a password"

        ui profile Desktop
          target platform web

          sample data
            active NordicDemo
            shape Customer maps to Accounts
            bind Tickets to HelpdeskDemo

        ui profile Phone
          sample data
            active blueprint crm-nordic-smb
        """;

    CompilationResult<ApplicationSyntax> _result;
    SampleDataSelectionSyntax _desktop;
    SampleDataSelectionSyntax _phone;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _desktop = _result.Value!.UiProfiles!.Single(profile => profile.Name == "Desktop").SampleData!;
        _phone = _result.Value!.UiProfiles!.Single(profile => profile.Name == "Phone").SampleData!;
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_select_the_active_pack_once_for_the_profile() => _desktop.Active!.Name.ShouldEqual("NordicDemo");
    [Fact] void should_select_an_own_pack_by_name() => _desktop.Active!.IsBlueprint.ShouldBeFalse();
    [Fact] void should_map_a_shape_to_a_read_model() => (_desktop.Mappings.Single().Shape, _desktop.Mappings.Single().ReadModel).ShouldEqual(("Customer", "Accounts"));
    [Fact] void should_bind_a_read_model_to_a_pack() => (_desktop.Bindings.Single().ReadModel, _desktop.Bindings.Single().Pack.Name).ShouldEqual(("Tickets", "HelpdeskDemo"));
    [Fact] void should_select_a_blueprint_pack() => (_phone.Active!.Name, _phone.Active.IsBlueprint).ShouldEqual(("crm-nordic-smb", true));
    [Fact] void should_not_check_a_blueprint_pack_against_the_document() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSampleDataPack).ShouldBeFalse();
}
