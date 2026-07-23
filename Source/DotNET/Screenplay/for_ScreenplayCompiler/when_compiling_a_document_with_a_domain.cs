// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_a_domain : given.a_compiler
{
    const string Source =
        """
        domain Sales.Invoicing

        import Customers.CustomerRegistered

        module Invoicing
          feature Invoices
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_domain() => _result.Value!.Domain!.Name.ShouldEqual("Sales.Invoicing");
}
