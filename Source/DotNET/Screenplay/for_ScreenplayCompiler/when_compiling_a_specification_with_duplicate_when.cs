// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_with_duplicate_when : given.a_compiler
{
    const string Source =
        """
        specification DuplicateWhen
          when RegisterInvoice
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          when CancelInvoice
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate_when() => _result.Diagnostics.Single().Message.ShouldEqual("Specification 'DuplicateWhen' already declares a 'when' - a specification can have at most one");
    [Fact] void should_still_have_the_first_when() => _result.Value!.When!.CommandType.ShouldEqual("RegisterInvoice");
}
