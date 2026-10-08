// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_event_routes;

public class integer_bounds : given.a_semantic_binder
{
    const string Source = """
        concept Key : Int
        eventsource Account
          identifier String
          stream Ledger
            streamId Key
        module M
          feature F
            slice StateChange S
              command C
                id String identifier
                key Key
                stream Account.Ledger
                  streamId = key
                produces event E
                  value String = id
        """;

    [Theory]
    [InlineData("9007199254740992", 9007199254740992L, false)]
    [InlineData("-9007199254740992", -9007199254740992L, false)]
    [InlineData("9007199254740993", 9007199254740992L, false)]
    [InlineData("-9007199254740993", -9007199254740992L, false)]
    [InlineData("9007199254740991", 9007199254740991L, true)]
    [InlineData("-9007199254740991", -9007199254740991L, true)]
    void should_never_round_a_command_route_literal_into_the_double_bound(string literal, long parsedValue, bool accepted)
    {
        var result = Bind(Source.Replace("streamId = key", $"streamId = {literal}", StringComparison.Ordinal));
        if (!accepted)
        {
            result.Success.ShouldBeFalse();
            result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeTrue();
            return;
        }
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var command = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        ((SemanticNumberValue)((SemanticValueExpression)command.Route!.StreamId!).Value).Value.ShouldEqual(parsedValue);
    }

    [Theory]
    [InlineData("9007199254740992", 9007199254740992L, false)]
    [InlineData("-9007199254740992", -9007199254740992L, false)]
    [InlineData("9007199254740993", 9007199254740992L, false)]
    [InlineData("-9007199254740993", -9007199254740992L, false)]
    [InlineData("9007199254740991", 9007199254740991L, true)]
    [InlineData("-9007199254740991", -9007199254740991L, true)]
    void should_preserve_a_routed_specification_input_and_refuse_it_at_runtime_outside_the_double_bound(string literal, long parsedValue, bool accepted)
    {
        var result = Bind(Source + $"\n      specification Input\n        when C\n          id = \"other\"\n          key = {literal}\n        then E\n          value = \"other\"\n");
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var compilation = result.Value!;
        var slice = compilation.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var key = slice.Commands.Single().Properties.Single(property => property.Name == "key");
        var specification = slice.Specifications.Single();
        ((SemanticNumberValue)specification.When!.Values.Single(value => value.TargetProperty == key.Id).Value).Value.ShouldEqual(parsedValue);
        var run = new SemanticSpecificationRunner().Run(compilation, specification.Id);
        if (!accepted)
        {
            run.Execution.ShouldBeOfExactType<SemanticRejected>();
            ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
            run.Execution.World.Facts.ShouldBeEmpty();
            return;
        }
        run.Passed.ShouldBeTrue();
        ((SemanticAccepted)run.Execution).Facts.Single().Route!.StreamId.ShouldEqual(literal);
    }
}
