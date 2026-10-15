// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_protected_reads : Specification
{
    static SemanticApplication Application() => canonical_serialization_golden_vectors.CreateSemanticModelV24().Application;

    static SemanticApplication WithCommand(SemanticApplication application, Func<SemanticCommand, SemanticCommand> transform) => application with
    {
        Modules = [.. application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Select(transform)] })]
            })]
        })]
    };

    static void Refuse(SemanticApplication application, SemanticVersion? version = null)
    {
        var selected = version ?? SemanticVersion.V24;
        Catch.Exception(() => ExecutableSemanticModel.Create(new(selected.Major, selected.Minor), selected, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_reject_the_mode_in_every_earlier_version()
    {
        var application = Application();
        EsmSchemaV22Support.SemanticVersions.Length.ShouldEqual(22);
        foreach (var version in EsmSchemaV22Support.SemanticVersions) Refuse(application, version);
    }

    [Fact] void should_require_the_preamble_at_v24() => Refuse(Application() with { CommandReadMode = null });
    [Fact] void should_refuse_unknown_modes() => Refuse(Application() with { CommandReadMode = (SemanticCommandReadMode)99 });
    [Fact] void should_refuse_reads_without_the_mode_in_an_earlier_version() => Refuse(Application() with { CommandReadMode = null }, SemanticVersion.V22);
    [Fact] void should_refuse_read_paths_in_an_earlier_version_even_without_read_entries() => Refuse(WithCommand(Application() with { CommandReadMode = null }, command => command with { Reads = [] }), SemanticVersion.V22);
    [Fact] void should_refuse_an_unknown_view() => Refuse(WithCommand(Application(), command => command with { Reads = [command.Reads[0] with { ReadModel = command.Id }] }));
    [Fact] void should_refuse_an_unknown_key() => Refuse(WithCommand(Application(), command => command with { Reads = [command.Reads[0] with { Key = command.Id }] }));
    [Fact] void should_refuse_unknown_presence() => Refuse(WithCommand(Application(), command => command with { Reads = [command.Reads[0] with { Presence = (SemanticDecisionReadPresence)99 }] }));
    [Fact] void should_refuse_a_duplicate_read_name() => Refuse(WithCommand(Application(), command => command with { Reads = [command.Reads[0], command.Reads[0]] }));
    [Fact] void should_refuse_an_undeclared_read_path() => Refuse(WithCommand(Application(), command => command with { Reads = [] }));

    [Fact]
    void should_refuse_read_paths_in_production_conditions()
    {
        Refuse(WithCommand(Application(), command => command with
        {
            Produces = [command.Produces[0] with { Condition = command.Produces[0].Mappings[0].Source }]
        }));
    }

    [Fact]
    void should_refuse_read_paths_in_produces_when()
    {
        Refuse(WithCommand(Application(), command => command with
        {
            Produces = [command.Produces[0] with { When = command.Requirements[0].Condition }]
        }));
    }

    [Fact]
    void should_refuse_read_paths_in_destinations()
    {
        Refuse(WithCommand(Application(), command => command with
        {
            Produces = [command.Produces[0] with { Destination = command.Produces[0].Mappings[0].Source }]
        }));
    }

    [Fact]
    void should_refuse_a_generated_lookup_key()
    {
        Refuse(WithCommand(Application(), command => command with
        {
            Properties = [.. command.Properties.Select(property => property.Id == command.Reads[0].Key ? property with { IsGenerated = true } : property)]
        }));
    }

    [Fact]
    void should_refuse_an_optional_lookup_key()
    {
        Refuse(WithCommand(Application(), command => command with
        {
            Properties = [.. command.Properties.Select(property => property.Id == command.Reads[0].Key ? property with { Type = property.Type with { IsOptional = true } } : property)]
        }));
    }

    [Fact]
    void should_refuse_a_collection_lookup_key()
    {
        Refuse(WithCommand(Application(), command => command with
        {
            Properties = [.. command.Properties.Select(property => property.Id == command.Reads[0].Key ? property with { Type = property.Type with { IsCollection = true } } : property)]
        }));
    }
}
