// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_resolving_a_contribution;

public class and_the_owning_module_declares_the_contribution_point : given.a_compiler
{
    // Two modules both declare a 'navbar contributes Navigation' slot. Invoicing's own contribution must
    // resolve against its own AppShell, and stop there, even though Reporting's ReportShell also matches -
    // a module with its own matching slot stops contributions inside it from bubbling further out.
    const string Source =
        """
        module Invoicing
          screen template AppShell
            navbar contributes Navigation
            main

          contribute to Navigation
            navigate to InvoiceList

          feature InvoiceManagement
            slice StateView InvoiceList
              screen InvoiceList

        module Reporting
          screen template ReportShell
            navbar contributes Navigation
            main
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_resolve_without_ambiguity() => _result.Diagnostics.ShouldBeEmpty();
}
