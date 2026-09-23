// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.for_EventContextCatalog.given;

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog;

public class when_holding_the_editor_catalog_to_it : Specification
{
    string _checkedIn;

    void Establish() => EventContextSurfaces.RegenerateWhenRequested();

    void Because() => _checkedIn = File.ReadAllText(EventContextSurfaces.EditorCatalogPath).ReplaceLineEndings("\n");

    [Fact] void should_match_the_rendering_of_the_catalog() => _checkedIn.ShouldEqual(EventContextSurfaces.RenderEditorCatalog());
}
