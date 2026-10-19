// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_discovering_refactorings;

public class and_an_inline_event_is_extractable : given.a_refactoring_workspace
{
    JsonElement[] _extractions = [];
    JsonElement[] _renames = [];

    void Because()
    {
        _extractions = Offers("extract-inline-event");
        _renames = Offers("rename-declaration");
    }

    [Fact] void should_offer_every_inline_event() => _extractions.Select(offer => offer.GetProperty("name").GetString()).ShouldContainOnly("Renamed", "Closed");
    [Fact] void should_describe_the_declaration_kind() => _extractions.All(offer => offer.GetProperty("declarationKind").GetString() == "EventSyntax").ShouldBeTrue();
    [Fact] void should_bind_the_subject_to_the_currentCurrentRevision() => _extractions.All(offer => offer.GetProperty("subject").GetProperty("revision").GetString() == CurrentRevision).ShouldBeTrue();
    [Fact] void should_locate_the_declaration() => _extractions.Single(offer => offer.GetProperty("name").GetString() == "Renamed").GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(6);
    [Fact] void should_offer_renaming_declarations() => _renames.Select(offer => offer.GetProperty("name").GetString()).ShouldContain("Rename", "Renamed", "ProjectArchived", "Naming", "Projects");
}
