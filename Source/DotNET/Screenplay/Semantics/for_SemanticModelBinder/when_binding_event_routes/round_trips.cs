// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_event_routes;

public class round_trips : given.a_semantic_binder
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_round_trip_bound_scalar_and_composite_routes(bool composite)
    {
        var source = """
            concept Key : Int
            eventsource Account
              id "StoredAccount"
              identifier String
              stream Ledger
                id "StoredLedger"
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
                  specification Command
                    given E
                      for "other"
                      stream Account.Ledger
                        streamId = 9007199254740990
                      value = "before"
                    when C
                      id = "other"
                      key = 9007199254740991
                    then E
                      for "other"
                      stream Account.Ledger
                        streamId = 9007199254740991
                      value = "other"
                  specification Append
                    when append E
                      for "other"
                      stream Account.Ledger
                        streamId = -9007199254740991
                      value = "after"
                    then E
                      stream Account.Ledger
                        streamId = -9007199254740991
                      value = "after"
            """;
        if (composite)
        {
            source = source.Replace("    streamId Key", "    streamId\n      first Key\n      second String", StringComparison.Ordinal)
                .Replace("          streamId = key", "          streamId\n            second = id\n            first = key", StringComparison.Ordinal);
            foreach (var value in new[] { "9007199254740990", "9007199254740991", "-9007199254740991" })
            {
                source = source.Replace($"            streamId = {value}\n", $"            streamId\n              second = \"other\"\n              first = {value}\n", StringComparison.Ordinal);
            }
        }
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = result.Value!.Model;
        var validated = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application);
        var bytes = SemanticModelSerializer.Serialize(validated);
        var read = SemanticModelSerializer.Deserialize(bytes);
        read.Revision.ShouldEqual(model.Revision);
        SemanticModelSerializer.Serialize(read).SequenceEqual(bytes).ShouldBeTrue();
        var slice = read.Application.Modules.Single().Features.Single().Slices.Single();
        slice.Commands.Single().Route.ShouldNotBeNull();
        slice.Specifications.Single(specification => specification.Name == "Command").GivenEvents.Single().Route.ShouldNotBeNull();
        slice.Specifications.Single(specification => specification.Name == "Append").WhenAppended!.Route.ShouldNotBeNull();
        slice.Specifications.All(specification => specification.ThenEvents.Single().Route is not null).ShouldBeTrue();
        if (composite) slice.Commands.Single().Route!.StreamIdParts.Select(part => part.Part).ShouldEqual(["first", "second"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_use_only_an_unambiguous_producer_fallback(bool ambiguous)
    {
        var source = """
            eventsource Account
              stream All
            module M
              feature F
                slice StateView S
                  event E
                    value String
                  command C
                    id String identifier
                    value String
                    produces E
                      for id
                      value = value
                  specification History
                    when append E
                      for "other"
                      stream Account.All
                      value = "after"
                    then E
                      stream Account.All
                      value = "after"
            """;
        if (ambiguous)
        {
            source += "\n      command Other\n        id Uuid identifier\n        value String\n        produces E\n          for id\n          value = value\n";
        }
        var result = Bind(source);
        if (ambiguous)
        {
            result.Success.ShouldBeFalse();
            result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("identifier on its event source", StringComparison.Ordinal)).ShouldBeTrue();
            return;
        }
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(result.Value!.Model));
        model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().WhenAppended!.EventSource!.Type
            .ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
    }
}
