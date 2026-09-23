// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.for_EventContextCatalog.given;

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog;

public class when_holding_the_documentation_table_to_it : Specification
{
    string _table;

    void Establish() => EventContextSurfaces.RegenerateWhenRequested();

    void Because() => _table = EventContextSurfaces.TableIn(File.ReadAllText(EventContextSurfaces.DocumentationPath).ReplaceLineEndings("\n"));

    [Fact] void should_carry_the_table() => _table.ShouldNotBeEmpty();
    [Fact] void should_match_the_rendering_of_the_catalog() => _table.ShouldEqual(EventContextSurfaces.RenderTable());
}
