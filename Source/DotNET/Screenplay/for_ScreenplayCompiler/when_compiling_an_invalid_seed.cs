// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_seed : given.a_compiler
{
    const string Source =
        """
        import Customers.CustomerRegistered

        seed
          for customers
            CustomerRegistered
              name = "Acme Corp"
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
            lowercase
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_invalid_group() => _result.Diagnostics.First().Message.ShouldEqual("Invalid seed group 'for customers' - expected 'for \"<event source id>\"'");
    [Fact] void should_report_the_invalid_event() => _result.Diagnostics.Last().Message.ShouldEqual("Invalid seed event 'lowercase' - expected an event type name");
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_valid_group() => _result.Value!.Seeds!.Single().Groups.Single().EventSourceId.ShouldEqual("9c858901-8a57-4791-81fe-4c455b099bc9");
}
