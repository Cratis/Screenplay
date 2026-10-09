// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_event_routes;

public class reaction_integer_bounds : given.a_semantic_binder
{
    [Theory]
    [InlineData(9007199254740991L, true, false)]
    [InlineData(-9007199254740991L, true, false)]
    [InlineData(9007199254740992L, false, false)]
    [InlineData(-9007199254740992L, false, false)]
    [InlineData(9007199254740991L, true, true)]
    [InlineData(-9007199254740991L, true, true)]
    [InlineData(9007199254740992L, false, true)]
    [InlineData(-9007199254740992L, false, true)]
    void should_lower_only_route_feeding_invocation_literals_losslessly_and_enforce_the_double_bound(long integer, bool accepted, bool composite)
    {
        var literal = integer.ToString(CultureInfo.InvariantCulture);
        var shape = composite ? "    streamId\n      key Key\n      period String" : "    streamId Key";
        var route = composite ? "          streamId\n            key = key\n            period = \"2026-10\"" : "          streamId = key";
        var result = Bind($$"""
            concept Key : Int
            eventsource Account
              identifier String
              stream Ledger
            {{shape}}
            module M
              feature F
                slice StateChange S
                  command C
                    id String identifier
                    key Key
                    payload Decimal
                    stream Account.Ledger
            {{route}}
                    produces event E
                      value Decimal = payload
                slice Automation A
                  reaction R
                    when Startup
                      invokes C
                        id = "other"
                        key = {{literal}}
                        payload = 9007199254740991
                  specification Invocation
                    when trigger Startup
                    then E
                      value = 9007199254740991
            """);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var compilation = result.Value!;
        var slices = compilation.Model.Application.Modules.Single().Features.Single().Slices;
        var command = slices.Single(slice => slice.Name == "S").Commands.Single();
        var automation = slices.Single(slice => slice.Name == "A");
        var mappings = automation.Reactions.Single().Triggers.Single().Invokes.Single().Mappings;
        var key = command.Properties.Single(property => property.Name == "key");
        ((SemanticNumberValue)((SemanticValueExpression)mappings.Single(mapping => mapping.TargetProperty == key.Id).Source).Value).Value.ShouldEqual(integer);
        var payload = command.Properties.Single(property => property.Name == "payload");
        ((SemanticNumberValue)((SemanticValueExpression)mappings.Single(mapping => mapping.TargetProperty == payload.Id).Source).Value).Value.ShouldEqual(9007199254740990m);
        var run = new SemanticSpecificationRunner().Run(compilation, automation.Specifications.Single().Id);
        if (!accepted)
        {
            run.Execution.ShouldBeOfExactType<SemanticRejected>();
            var rejected = (SemanticRejected)run.Execution;
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
            rejected.World.Facts.ShouldBeEmpty();
            return;
        }
        run.Passed.ShouldBeTrue();
        ((SemanticAccepted)run.Execution).Facts.Single().Route!.StreamId.ShouldEqual(composite ? literal + "|2026-10" : literal);
    }
}
