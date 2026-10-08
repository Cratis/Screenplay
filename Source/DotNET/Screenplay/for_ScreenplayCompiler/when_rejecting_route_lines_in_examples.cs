// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_route_lines_in_examples
{
    [Theory]
    [InlineData("stream Account.Events\n    streamId = \"partition\"")]
    [InlineData("no stream")]
    void should_reject_routes_on_command_examples(string route)
    {
        var result = new ScreenplayCompiler().Compile($"example Fixture : Act\n  {route}\n  amount = 1\nmodule M\n  feature F\n    slice StateChange S\n      command Act\n        amount Int");
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationExampleBody);
        diagnostic.Location.Line.ShouldEqual(2);
        diagnostic.Message.ShouldEqual("only event examples carry routes; a command's route comes from its declaration, and read models have none");
        result.Value!.Examples.Single().Values.Single().Property.ShouldEqual("amount");
    }
}
