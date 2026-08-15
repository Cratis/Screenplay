// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_contribution : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          layout AppShell
            template
              navbar contributes Navigation
              main

          contribute to Navigation
            navigate to InvoiceList
            label "Invoices"
            order 10

          feature InvoiceManagement
            slice StateView InvoiceList
              screen InvoiceList

            contribute to Navigation
              navigate to InvoiceList
              label "Adjustments"
              order 20
        """;

    CompilationResult<ApplicationSyntax> _result;
    SlotSyntax _slot;
    ContributionSyntax _moduleContribution;
    ContributionSyntax _featureContribution;

    void Because()
    {
        _result = _compiler.Compile(Source);
        var module = _result.Value!.Modules.Single();
        _slot = module.Layouts.Single().Slots.First();
        _moduleContribution = module.Contributions!.Single();
        _featureContribution = module.Features.Single().Contributions!.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_slot_name() => _slot.Name.ShouldEqual("navbar");
    [Fact] void should_parse_the_slot_contribution_point() => _slot.Contributes.ShouldEqual("Navigation");
    [Fact] void should_parse_the_module_contribution_point() => _moduleContribution.ContributionPoint.ShouldEqual("Navigation");
    [Fact] void should_parse_the_module_contribution_navigate_target() => _moduleContribution.Navigate!.Screen.ShouldEqual("InvoiceList");
    [Fact] void should_parse_the_module_contribution_label() => _moduleContribution.Label.ShouldEqual("Invoices");
    [Fact] void should_parse_the_module_contribution_order() => _moduleContribution.Order.ShouldEqual(10);
    [Fact] void should_parse_the_feature_contribution_point() => _featureContribution.ContributionPoint.ShouldEqual("Navigation");
    [Fact] void should_parse_the_feature_contribution_label() => _featureContribution.Label.ShouldEqual("Adjustments");
    [Fact] void should_parse_the_feature_contribution_order() => _featureContribution.Order.ShouldEqual(20);
}
