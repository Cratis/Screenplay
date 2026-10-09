// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_validating_programmatic_event_routes : Specification
{
    internal static SemanticApplication Application => canonical_serialization_golden_vectors.CreateEventRoutesApplication();
    internal static SemanticSlice Slice(SemanticApplication application) => application.Modules[0].Features[^1].Slices[0];
    internal static SemanticApplication WithSlice(SemanticApplication application, SemanticSlice slice) => application with
    {
        Modules = application.Modules.SetItem(0, application.Modules[0] with
        {
            Features = application.Modules[0].Features.SetItem(application.Modules[0].Features.Length - 1, application.Modules[0].Features[^1] with { Slices = [slice] })
        })
    };
    internal static ExecutableSemanticModel Create(SemanticApplication application) => ExecutableSemanticModel.Create(LanguageVersion.V8, SemanticVersion.V8, application);

    [Fact]
    void should_accept_routes_and_history_without_a_producer() => Create(Application).Application.EventSources.Length.ShouldEqual(2);

    [Fact]
    void should_type_history_by_its_source_even_when_a_producer_disagrees()
    {
        var application = Application;
        var slice = Slice(application);
        var source = new SemanticEventSource(SemanticId.Parse($"sem1:{9500:x64}"), "External", "External", [new(SemanticId.Parse($"sem1:{9501:x64}"), "All", "All")])
        {
            IdentifierType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)
        };
        var fixture = new SemanticSpecificationEvent(slice.Events[0].Id, [])
        {
            EventSource = new(source.IdentifierType, SemanticValue.Text("external-source")),
            Route = new(source.Id, source.Streams[0].Id)
        };
        var specification = slice.Specifications[0] with { GivenEvents = [fixture] };
        Create(WithSlice(application with { EventSources = application.EventSources.Add(source) }, slice with
        {
            Specifications = slice.Specifications.SetItem(0, specification)
        })).Application.EventSources.Length.ShouldEqual(3);
    }

    [Fact]
    void should_refuse_routes_at_every_earlier_version()
    {
        var application = Application;
        for (uint major = 1; major <= 7; major++)
        {
            Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(new(major, 0), new(major, 0), application));
        }
    }

    [Fact]
    void should_require_activation() => Assert.Throws<InvalidSemanticContract>(() => Create(canonical_serialization_golden_vectors.CreateSemanticModelV7().Application));

    [Fact]
    void should_refuse_malformed_declarations()
    {
        var application = Application;
        var source = application.EventSources[1];
        var stream = source.Streams[0];
        var invalidSources = new[]
        {
            source with { SourceKind = "Default" },
            source with { SourceKind = "" },
            source with { Id = default },
            source with { SourceKind = application.EventSources[0].SourceKind },
            source with { Streams = default },
            source with { Streams = source.Streams.Add(stream with { Id = source.Streams[1].Id }) },
            source with { Streams = source.Streams.SetItem(0, stream with { StreamKind = source.Streams[1].StreamKind }) },
            source with { Streams = source.Streams.SetItem(0, stream with { StreamIdParts = [new("a", stream.StreamIdType!), new("b", stream.StreamIdType!)] }) },
            source with { Streams = source.Streams.SetItem(0, stream with { StreamIdType = null, StreamIdParts = [new("a", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))] }) },
            source with { Streams = source.Streams.SetItem(0, stream with { StreamIdType = null, StreamIdParts = [new("a", stream.StreamIdType!), new("a", stream.StreamIdType!)] }) },
            source with { Streams = source.Streams.SetItem(0, stream with { StreamIdType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean) }) },
            source with { Streams = source.Streams.SetItem(0, stream with { StreamIdParts = default }) }
        };
        foreach (var invalid in invalidSources) Assert.Throws<InvalidSemanticContract>(() => Create(application with { EventSources = application.EventSources.SetItem(1, invalid) }));
    }

    [Fact]
    void should_refuse_malformed_command_mappings()
    {
        var application = Application;
        var slice = Slice(application);
        var command = slice.Commands[0];
        var route = command.Route!;
        var foreign = application.EventSources[0];
        var invalidRoutes = new[]
        {
            route with { StreamId = null },
            route with { Stream = foreign.Streams[0].Id },
            route with { Source = default },
            route with { StreamIdParts = [new("extra", route.StreamId!)] },
            route with { StreamIdParts = default },
            route with { StreamId = SemanticExpression.Property(SemanticExpressionRootKind.Event, command.Properties[1].Id) },
            route with { StreamId = SemanticExpression.FromValue(SemanticValue.Text("")) },
            route with { StreamId = SemanticExpression.FromValue(SemanticValue.Number(1)) }
        };
        foreach (var invalid in invalidRoutes)
        {
            Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with { Commands = slice.Commands.SetItem(0, command with { Route = invalid }) })));
        }
        foreach (var property in new[]
        {
            command.Properties[1] with { IsGenerated = true },
            command.Properties[1] with { Type = command.Properties[1].Type with { IsOptional = true } },
            command.Properties[1] with { Type = command.Properties[1].Type with { IsCollection = true } },
            command.Properties[1] with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) }
        })
        {
            Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with { Commands = slice.Commands.SetItem(0, command with { Properties = command.Properties.SetItem(1, property), Response = null }) })));
        }
        var composite = slice.Commands[^1];
        foreach (var parts in new[] { composite.Route!.StreamIdParts.RemoveAt(0), composite.Route.StreamIdParts.Add(composite.Route.StreamIdParts[0]), [.. composite.Route.StreamIdParts.Reverse()] })
        {
            Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with { Commands = slice.Commands.SetItem(slice.Commands.Length - 1, composite with { Route = composite.Route with { StreamIdParts = parts } }) })));
        }
    }

    [Fact]
    void should_refuse_a_command_scalar_literal_with_a_mismatched_value_kind()
    {
        var application = Application;
        var slice = Slice(application);
        var command = slice.Commands[1];
        var route = command.Route! with
        {
            StreamId = SemanticExpression.FromValue(new SemanticTextValue("x") with { Kind = SemanticValueKind.Number })
        };
        Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with
        {
            Commands = slice.Commands.SetItem(1, command with { Route = route })
        })));
    }

    [Fact]
    void should_refuse_a_command_composite_part_literal_with_a_mismatched_value_kind()
    {
        var application = Application;
        var slice = Slice(application);
        var command = slice.Commands[^1];
        var route = command.Route! with
        {
            StreamIdParts = command.Route.StreamIdParts.SetItem(1, command.Route.StreamIdParts[1] with
            {
                Value = SemanticExpression.FromValue(new SemanticTextValue("x") with { Kind = SemanticValueKind.Number })
            })
        };
        Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with
        {
            Commands = slice.Commands.SetItem(slice.Commands.Length - 1, command with { Route = route })
        })));
    }

    [Fact]
    void should_refuse_a_fixture_scalar_with_a_mismatched_value_kind()
    {
        var route = Slice(Application).Specifications[0].GivenEvents[0].Route! with
        {
            StreamId = new SemanticTextValue("0b4f8e6c-1d6a-4a52-9a53-3f5b6a0c1d11") with { Kind = SemanticValueKind.Number }
        };
        RejectFixtureRoute(0, route);
    }

    [Fact]
    void should_refuse_a_fixture_part_with_a_mismatched_value_kind()
    {
        var specifications = Slice(Application).Specifications;
        var route = specifications[^1].GivenEvents[0].Route!;
        RejectFixtureRoute(specifications.Length - 1, route with
        {
            StreamIdParts = route.StreamIdParts.SetItem(1, route.StreamIdParts[1] with
            {
                Value = new SemanticTextValue("x") with { Kind = SemanticValueKind.Number }
            })
        });
    }

    static void RejectFixtureRoute(int specificationIndex, SemanticFixtureRoute route)
    {
        var application = Application;
        var slice = Slice(application);
        var specification = slice.Specifications[specificationIndex];
        var fixture = specification.GivenEvents[0];
        foreach (var invalid in new[]
        {
            specification with { GivenEvents = [fixture with { Route = route }] },
            specification with { When = null, WhenAppended = new(fixture.EventContract, fixture.Values) { EventSource = fixture.EventSource, Route = route } },
            specification with { ThenEvents = [fixture with { Route = route }] }
        })
        {
            Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with
            {
                Specifications = slice.Specifications.SetItem(specificationIndex, invalid)
            })));
        }
    }

    [Fact]
    void should_refuse_malformed_fixture_routes()
    {
        var application = Application;
        var slice = Slice(application);
        var specification = slice.Specifications[0];
        var value = specification.GivenEvents[0];
        foreach (var invalid in new[]
        {
            value with { Unrouted = true },
            value with { EventSource = null },
            value with { Route = value.Route! with { StreamId = null } },
            value with { Route = value.Route! with { StreamId = SemanticValue.Text("not a uuid") } },
            value with { Route = value.Route! with { StreamIdParts = [new("extra", SemanticValue.Text("x"))] } },
            value with { Route = value.Route! with { Stream = application.EventSources[0].Streams[0].Id } },
            value with { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("other")) }
        })
        {
            Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with { Specifications = slice.Specifications.SetItem(0, specification with { GivenEvents = [invalid] }) })));
        }
        Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with { Specifications = slice.Specifications.SetItem(0, specification with { ThenEvents = [specification.ThenEvents[0] with { Unrouted = true }] }) })));
        var composite = slice.Specifications[^1];
        var compositeFixture = composite.GivenEvents[0];
        foreach (var invalid in new[]
        {
            compositeFixture with { Route = compositeFixture.Route! with { StreamIdParts = compositeFixture.Route.StreamIdParts.RemoveAt(0) } },
            compositeFixture with { Route = compositeFixture.Route! with { StreamIdParts = compositeFixture.Route.StreamIdParts.Add(compositeFixture.Route.StreamIdParts[0]) } },
            compositeFixture with { Route = compositeFixture.Route! with { StreamId = SemanticValue.Text("pre-joined") } },
            compositeFixture with { Route = compositeFixture.Route! with { StreamIdParts = default } }
        })
        {
            Assert.Throws<InvalidSemanticContract>(() => Create(WithSlice(application, slice with
            {
                Specifications = slice.Specifications.SetItem(slice.Specifications.Length - 1, composite with { GivenEvents = [invalid] })
            })));
        }
        var untyped = application.EventSources[1] with { IdentifierType = null };
        Assert.Throws<InvalidSemanticContract>(() => Create(application with { EventSources = application.EventSources.SetItem(1, untyped) }));
    }
}
