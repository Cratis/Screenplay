// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_specification_uses_a_pending_answer : given.a_settlement_with_an_ability
{
    Exception _caught;

    void Establish() => PrepareAbility(extra: """
                  specification Answer
                    given Carrier.Send returns
                      value = "ok"
                    when append Started
                      for "one"
                    then Continued
                      for "one"
                      value = "ok"
            """);

    void Because()
    {
        var model = _plan.Model;
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification with { AbilityAnswers = [Answer(SemanticAbilityAnswerKind.Pending)] })]
                    })]
                })]
            })]
        };
        try { SemanticModelValidator.Validate(application, model.SemanticVersion); }
        catch (InvalidSemanticContract exception) { _caught = exception; }
    }

    [Fact] void should_reject_a_host_only_answer_in_a_specification() => _caught.ShouldNotBeNull();
}
