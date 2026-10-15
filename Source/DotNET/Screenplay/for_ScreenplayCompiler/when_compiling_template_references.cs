// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_template_references : given.a_compiler
{
    const string Source =
        """
        module Sales
          screen template OrderList
            category List
            scopes slice stateview
            body

          dialog template ConfirmOrder
            category Dialog
            scopes slice statechange
            body

          template blueprint Crm Workspace version "^1.4"

          feature Orders
            template blueprint Crm Pipeline

            slice StateView OrderList
              template OrderList

              screen Orders
                template blueprint Crm CustomerDetail version "~1.4.2"
                  body
                    title "Orders"

            slice StateChange PlaceOrder
              screen PlaceOrder
                template blueprint Crm ConfirmOrder
                  body
                    title "Place order"

        ui profile Desktop
          packages
            Crm
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    ModuleSyntax Module => _result.Value!.Modules.Single();

    IEnumerable<ScreenTemplateReferenceSyntax> ScreenReferences => Module.Features.Single().Slices
        .SelectMany(slice => slice.Screens).SelectMany(screen => screen.Directives).OfType<ScreenTemplateReferenceSyntax>();

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_an_own_assignment_without_a_blueprint() => Module.Features.Single().Slices.First().Templates.Single().Blueprint.ShouldBeNull();
    [Fact] void should_read_a_module_blueprint_assignment() => (Module.Templates.Single().Blueprint, Module.Templates.Single().Name, Module.Templates.Single().Version).ShouldEqual(("Crm", "Workspace", "^1.4"));
    [Fact] void should_read_a_feature_blueprint_assignment_without_a_version() => (Module.Features.Single().Templates.Single().Blueprint, Module.Features.Single().Templates.Single().Version).ShouldEqual(("Crm", null));
    [Fact] void should_read_a_screen_blueprint_reference_with_its_version() => (ScreenReferences.First().Blueprint, ScreenReferences.First().Name, ScreenReferences.First().Version).ShouldEqual(("Crm", "CustomerDetail", "~1.4.2"));
    [Fact] void should_read_the_slice_kinds_of_a_scope() => Module.ScreenTemplates.Single().SliceKinds.ShouldContainOnly("StateView");
    [Fact] void should_keep_the_slice_scope_beside_its_kinds() => Module.DialogTemplates!.Single().Scopes.ShouldContainOnly("slice");
}
