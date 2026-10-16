// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons.given;

public class a_date_command : Specification
{
    protected SemanticApplication Application;
    protected SemanticCommand Command;
    protected SemanticProperty First;
    protected SemanticProperty Second;
    protected SemanticProperty Unused;

    void Establish()
    {
        Application = new(default, "Calendar", [], [], []);
        First = new(SemanticId.Create(SemanticKind.Property, "first"), "from", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime), false);
        Second = new(SemanticId.Create(SemanticKind.Property, "second"), "to", First.Type, false);
        Unused = new(SemanticId.Create(SemanticKind.Property, "unused"), "description", First.Type, false);
        Command = new(default, "KeepWindow", [First, Second, Unused], [], [])
        {
            Requirements = [new(new SemanticComparison(new(Second.Id, null), SemanticComparisonOperator.GreaterThanOrEqual, new(First.Id, null)), null)]
        };
    }
}
