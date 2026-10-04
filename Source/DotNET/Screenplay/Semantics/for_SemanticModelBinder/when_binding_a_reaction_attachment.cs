// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_reaction_attachment : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;
    CompilationResult<SemanticCompilation> _withoutAttachment;

    void Because()
    {
        _result = Bind(
            """
            module Invoicing
              feature Invoices
                slice Automation Notify
                  reaction Notifier
                    when SomethingHappened
                      file Reactions/Notifier.cs
                  event SomethingHappened
                    note String
            """);
        _withoutAttachment = Bind(
            """
            module Invoicing
              feature Invoices
                slice Automation Notify
                  reaction Notifier
                    when SomethingHappened
                  event SomethingHappened
                    note String
            """);
    }

    [Fact] void should_bind_the_reaction() => _result.Success.ShouldBeTrue();
    [Fact] void should_list_the_reaction_effect() => _result.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.ReactionEffect);
    [Fact] void should_keep_the_attachment_path() => _result.ImplementationRequirements.Single().File.ShouldEqual("Reactions/Notifier.cs");
    [Fact] void should_leave_the_body_to_a_target() => Trigger(_result).RequirementId.ShouldEqual(_result.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_not_require_code_from_a_bodyless_reaction() => _withoutAttachment.ImplementationRequirements.ShouldBeEmpty();
    [Fact] void should_bind_a_bodyless_reaction_without_a_requirement() => Trigger(_withoutAttachment).RequirementId.ShouldBeNull();

    static SemanticReactionTrigger Trigger(CompilationResult<SemanticCompilation> result) =>
        result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single().Triggers.Single();
}
