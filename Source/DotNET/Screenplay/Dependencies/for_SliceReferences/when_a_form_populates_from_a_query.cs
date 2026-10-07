// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Dependencies.for_SliceReferences;

public class when_a_form_populates_from_a_query : Specification
{
    FormSyntax _form;
    SliceReferences _collector;

    void Establish()
    {
        _form = new ScreenplayCompiler().Parse("module M\n  form Edit for C\n    populate via query Q\n", "form.play").Value!.Modules.Single().Forms!.Single();
        _collector = new();
    }
    void Because() => _collector.VisitForm(_form);

    [Fact] void should_classify_the_query_as_shows() => _collector.References.Single(reference => reference.Role == "populate").Kind.ShouldEqual("shows");
    [Fact] void should_retain_the_query_name() => _collector.References.Single(reference => reference.Role == "populate").Name.ShouldEqual("Q");
    [Fact] void should_resolve_against_queries() => _collector.References.Single(reference => reference.Role == "populate").TargetKind.ShouldEqual("Query");
    [Fact] void should_retain_the_source_location() => _collector.References.Single(reference => reference.Role == "populate").Location.ShouldEqual(_form.Populate!.Location);
}
