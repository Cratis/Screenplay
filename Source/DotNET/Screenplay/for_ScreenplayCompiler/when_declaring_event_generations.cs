// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_declaring_event_generations : given.a_compiler
{
    const string Prefix = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n";

    [Fact]
    void should_parse_complete_generations_and_keep_the_explicit_marker()
    {
        var result = _compiler.Compile(Prefix + "      event ProjectRegistered generation 1\n        projectId Uuid\n      event ProjectRegistered generation 2\n        name String\n");
        result.Success.ShouldBeTrue();
        var events = result.Value!.Modules.Single().Features.Single().Slices.Single().Events.ToArray();
        events.Select(@event => @event.Generation).ShouldEqual(1u, 2u);
        events.All(@event => @event.HasGenerationMarker).ShouldBeTrue();
        events[0].Properties.Single().Name.ShouldEqual("projectId");
        events[1].Properties.Single().Name.ShouldEqual("name");
    }

    [Fact]
    void should_resolve_both_generations_to_one_catalog_contract_identity()
    {
        var result = _compiler.Compile(Prefix + "      event ProjectRegistered generation 1\n        projectId Uuid\n      event ProjectRegistered generation 2\n        name String\n");
        result.Success.ShouldBeTrue();
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var ids = result.Value!.Modules.Single().Features.Single().Slices.Single().Events
            .Select(@event => catalog.ResolveEventContract(SemanticAddress.ForEventContract(slice, @event.Name)).Id)
            .ToArray();
        ids[0].ShouldEqual(ids[1]);
    }

    [Fact]
    void should_not_treat_a_same_named_event_in_another_slice_as_generation_one()
    {
        const string source = Prefix + "      event ProjectRegistered generation 1\n        projectId Uuid\n" +
            "    slice StateChange ImportProject\n      event ProjectRegistered generation 2\n        name String\n";
        var result = _compiler.Compile(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MissingEventGeneration).ShouldBeTrue();
    }

    [Fact]
    void should_default_an_unmarked_event_to_generation_one()
    {
        var result = _compiler.Compile(Prefix + "      event ProjectRegistered\n        projectId Uuid\n");
        result.Success.ShouldBeTrue();
        var @event = result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single();
        @event.Generation.ShouldEqual(1u);
        @event.HasGenerationMarker.ShouldBeFalse();
    }

    [Fact]
    void should_reject_missing_generation_one()
    {
        var result = _compiler.Compile(Prefix + "      event ProjectRegistered generation 2\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MissingEventGeneration).ShouldBeTrue();
    }

    [Fact]
    void should_reject_a_gap_between_generations()
    {
        var result = _compiler.Compile(Prefix + "      event ProjectRegistered generation 1\n        projectId Uuid\n      event ProjectRegistered generation 3\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MissingEventGeneration).ShouldBeTrue();
    }

    [Fact]
    void should_reject_duplicate_generations()
    {
        var result = _compiler.Compile(Prefix + "      event ProjectRegistered generation 1\n        projectId Uuid\n      event ProjectRegistered generation 1\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateEventGeneration).ShouldBeTrue();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4294967295")]
    [InlineData("4294967296")]
    void should_reject_out_of_range_generations(string generation)
    {
        var result = _compiler.Compile(Prefix + $"      event ProjectRegistered generation {generation}\n        name String\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidEventGeneration).ShouldBeTrue();
    }
}
