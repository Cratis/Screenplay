// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Dependencies.for_SliceReferences;

public class when_a_form_targets_a_command : Specification
{
    FormSyntax _form;
    SliceReferences _collector;

    void Establish()
    {
        _form = new ScreenplayCompiler().Parse("module M\n  form Edit for C\n", "form.play").Value!.Modules.Single().Forms!.Single();
        _collector = new();
    }
    void Because() => _collector.VisitForm(_form);

    [Fact] void should_classify_the_command_as_asks() => _collector.References.Single().Kind.ShouldEqual("asks");
    [Fact] void should_retain_the_form_command_role() => _collector.References.Single().Role.ShouldEqual("formCommand");
    [Fact] void should_retain_the_command_name() => _collector.References.Single().Name.ShouldEqual("C");
    [Fact] void should_resolve_against_commands() => _collector.References.Single().TargetKind.ShouldEqual("Command");
    [Fact] void should_retain_the_source_location() => _collector.References.Single().Location.ShouldEqual(_form.Location);
}
