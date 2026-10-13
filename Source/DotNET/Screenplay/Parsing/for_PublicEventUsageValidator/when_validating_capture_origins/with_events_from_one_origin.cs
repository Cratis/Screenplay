// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_capture_origins;

public class with_events_from_one_origin : Specification
{
    Syntax.ApplicationSyntax _application;
    ParserContext _context;

    void Establish()
    {
        _application = new ScreenplayCompiler().Parse("import Outside.First from \"store\"\nimport Outside.Second from \"store\"\nmodule M\n  feature F\n    slice Translate Receive\n      direction inbound\n      capture Feed\n        source events\n          from First\n          from Second\n        key id\n").Value!;
        _context = ParserContext.ForDiagnostics();
    }

    void Because() => PublicEventUsageValidator.Validate(_application, _context);

    [Fact] void should_accept_the_capture() => _context.Diagnostics.ShouldBeEmpty();
}
