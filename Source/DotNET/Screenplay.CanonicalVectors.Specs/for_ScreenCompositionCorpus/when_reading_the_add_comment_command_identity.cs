// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The browser harness asserts that a native AddComment form exposes a commentId input. That expectation comes from the
/// authored model, not from what a runtime happens to render: the command declares a client-supplied identifier rather than
/// a generated one, and its own specification supplies the identifier when it executes the command.
/// </summary>
public class when_reading_the_add_comment_command_identity : Specification
{
    CommandSyntax _command = null!;
    SpecificationCommandSyntax _specifiedExecution = null!;

    void Because()
    {
        var document = ScreenCompositionCorpus.V1.SourceForms.Single(form => form.Name == "folder").Documents.Single(document => document.StableKey == "add-comment-slice");
        var parsed = new ScreenplayCompiler().Parse(document.Text);
        var slice = parsed.Value!.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Commands.Any(command => command.Name == "AddComment"));
        _command = slice.Commands.Single(command => command.Name == "AddComment");
        _specifiedExecution = slice.Specifications.Single(specification => specification.Name == "AddingAComment").When!;
    }

    [Fact] void should_declare_comment_id_as_the_command_identifier() => _command.Properties.Single(property => property.Name == "commentId").IsIdentifier.ShouldBeTrue();
    [Fact] void should_not_generate_the_comment_id() => _command.Properties.Single(property => property.Name == "commentId").IsGenerated.ShouldBeFalse();
    [Fact] void should_not_generate_any_add_comment_input() => _command.Properties.Any(property => property.IsGenerated).ShouldBeFalse();
    [Fact] void should_supply_the_comment_id_when_the_authored_specification_executes_the_command() => _specifiedExecution.Values.Select(value => value.Property).ShouldContain("commentId");
}
