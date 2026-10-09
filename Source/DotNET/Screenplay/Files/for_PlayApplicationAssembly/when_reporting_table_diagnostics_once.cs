// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayApplicationAssembly;

public class when_reporting_table_diagnostics_once : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var documents = new Dictionary<string, string>
        {
            ["application.play"] = "eventsource Account\n  identifier String\n  stream All\nimport \"table.play\"",
            ["table.play"] = "module M\n  feature F\n    slice StateView S\n      event E\n      specification Appending\n        parameter unused Int\n        case One unused = 1\n        case Two unused = 2\n        case Three unused = 3\n        when append E\n          stream Account.All\n        then E"
        };
        (_, _result) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["application.play"], new InMemoryPlayDocumentSource(documents));
    }

    [Fact] void should_report_an_unused_parameter_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnusedSpecificationParameter).ShouldEqual(1);
    [Fact] void should_report_the_authored_route_error_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationStreamEventSource).ShouldEqual(1);
    [Fact] void should_keep_the_route_errors_document() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationStreamEventSource).Location.Path.ShouldEqual("table.play");

    [Fact]
    void should_report_a_bad_case_route_value_once_across_two_steps()
    {
        var source = "eventsource Account\n  identifier String\n  stream All\n    streamId String\nmodule M\n  feature F\n    slice StateView S\n      event E\n      specification Appending\n        parameter key String\n        case Bad key = \"e\u0301\"\n        given E\n          for \"event\"\n          stream Account.All\n            streamId = case.key\n        when append E\n          for \"event\"\n          stream Account.All\n            streamId = case.key\n        then E";
        var result = new ScreenplayCompiler().Compile(source);
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationStreamRoute);
        diagnostic.Location.Line.ShouldEqual(11);
        diagnostic.Message.ShouldContain("Case 'Bad'");
    }
}
