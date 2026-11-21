// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.given;

public static class invalid_ability_answers
{
    static readonly Dictionary<string, Func<SemanticAbilityAnswer, SemanticAbilityAnswer>> _variants = new(StringComparer.Ordinal)
    {
        ["system"] = valid => valid with { System = default },
        ["ability"] = valid => valid with { Ability = "Other" },
        ["kind"] = valid => valid with { Kind = (SemanticAbilityAnswerKind)999 },
        ["undeclared-outcome"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Outcome, Outcome = "Unknown", Values = [] },
        ["missing-outcome"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Outcome, Values = [] },
        ["outcome-on-return"] = valid => valid with { Outcome = "Busy" },
        ["outcome-on-failure"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Failed, Outcome = "Busy", Values = [] },
        ["reason-on-return"] = valid => valid with { Reason = "invalid" },
        ["reason-on-outcome"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Outcome, Outcome = "Busy", Reason = "invalid", Values = [] },
        ["values-on-failure"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Failed },
        ["values-on-outcome"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Outcome, Outcome = "Busy" },
        ["default-values"] = valid => valid with { Values = default },
        ["undeclared-field"] = valid => valid with { Values = [new("other", SemanticValue.Text("invalid"))] },
        ["duplicate-field"] = valid => valid with { Values = [.. valid.Values, .. valid.Values] },
        ["wrong-value-type"] = valid => valid with { Values = [new("value", SemanticValue.Boolean(true))] },
        ["pending-reason"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Pending, Reason = "invalid", Values = [] },
        ["pending-values"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Pending },
        ["pending-outcome"] = valid => valid with { Kind = SemanticAbilityAnswerKind.Pending, Outcome = "Busy", Values = [] }
    };

    public static IEnumerable<object[]> Cases => _variants.Keys.Select(defect => new object[] { defect });

    public static SemanticAbilityAnswer Create(SemanticAbilityAnswer valid, string defect) => _variants[defect](valid);
}
