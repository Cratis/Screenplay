// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.given;

public class a_possible_events_handler : for_SemanticBodyExecution.given.a_possible_events_handler
{
    private protected SemanticHandlerResultOutcome Outcome = null!;
    protected int MaximumItems = SemanticHandlerResultValidation.DefaultMaximumItems;

    private protected void Validate() => Outcome = SemanticHandlerResultValidation.Validate(Plan, Command, Values, Answer, MaximumItems);
}
