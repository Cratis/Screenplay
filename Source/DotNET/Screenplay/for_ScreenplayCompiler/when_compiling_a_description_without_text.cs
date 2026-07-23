// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_description_without_text : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          description
          feature InvoiceManagement
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_missing_fence() => _result.Diagnostics.Single().Message.ShouldEqual("Expected an opening ``` fence after 'description'");
    [Fact] void should_have_no_description() => _result.Value!.Modules.Single().Description.ShouldBeNull();
    [Fact] void should_keep_the_feature() => _result.Value!.Modules.Single().Features.Single().Name.ShouldEqual("InvoiceManagement");
}
