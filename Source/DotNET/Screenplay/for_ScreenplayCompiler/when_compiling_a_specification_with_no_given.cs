// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_with_no_given : given.a_compiler
{
    const string Source =
        """
        specification RejectingAnInvoiceWithNoLines
          when RegisterInvoice
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then error "An invoice must have at least one line"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_given_events() => _result.Value!.Given.ShouldBeEmpty();
    [Fact] void should_have_the_when() => _result.Value!.When!.CommandType.ShouldEqual("RegisterInvoice");
    [Fact] void should_have_no_then_events() => _result.Value!.ThenEvents.ShouldBeEmpty();
    [Fact] void should_have_the_then_error() => _result.Value!.ThenErrors.Single().Name.ShouldEqual("An invoice must have at least one line");
}
