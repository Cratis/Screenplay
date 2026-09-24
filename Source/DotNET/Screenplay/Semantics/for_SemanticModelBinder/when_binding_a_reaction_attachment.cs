// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

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
            """);
        _withoutAttachment = Bind(
            """
            module Invoicing
              feature Invoices
                slice Automation Notify
                  reaction Notifier
                    when SomethingHappened
            """);
    }

    [Fact] void should_remain_unexecutable() => _result.Success.ShouldBeFalse();
    [Fact] void should_preserve_the_reaction_rejection() => _result.Diagnostics.Any(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax && value.Message.Contains("Reaction 'Notifier'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_list_the_reaction_effect() => _result.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.ReactionEffect);
    [Fact] void should_keep_the_attachment_path() => _result.ImplementationRequirements.Single().File.ShouldEqual("Reactions/Notifier.cs");
    [Fact] void should_not_require_code_from_a_bodyless_reaction() => _withoutAttachment.ImplementationRequirements.ShouldBeEmpty();
}
