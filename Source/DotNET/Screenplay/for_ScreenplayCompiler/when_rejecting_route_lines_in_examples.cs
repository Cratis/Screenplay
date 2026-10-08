// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_route_lines_in_examples
{
    [Theory]
    [InlineData("stream Account.Events\n    streamId = \"partition\"")]
    [InlineData("streamId = \"partition\"")]
    [InlineData("no stream")]
    void should_reject_the_route_and_recover_at_the_next_property(string route)
    {
        var result = new ScreenplayCompiler().Parse($"example Fixture : Happened\n  {route}\n  amount = 1");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidSpecificationExampleBody);
        result.Diagnostics.Single().Location.Line.ShouldEqual(2);
        result.Diagnostics.Single().Message.ShouldContain("state the route on the specification step");
        result.Value!.Examples.Single().Values.Single().Property.ShouldEqual("amount");
    }
}
