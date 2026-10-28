// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticRuleBodyExecution(ISemanticBodyExecutionProvider provider, IEnumerable<SemanticImplementationBody> bodies)
{
    readonly Dictionary<string, SemanticImplementationBody> _bodies = bodies.GroupBy(body => body.Requirement.RequirementId, StringComparer.Ordinal)
        .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);

    internal static SemanticRuleCheck Unsupported(SemanticValidationRule rule, string diagnostic) =>
        new(SemanticRuleStatus.NotEvaluable, rule, $"Rule body (requirement {rule.RequirementId}): {diagnostic}");

    internal SemanticRuleCheck Check(SemanticValidationRule rule, SemanticRuleContext context)
    {
        if (rule.RequirementId is not { } id || !_bodies.TryGetValue(id, out var body) || body.Requirement.Role != SemanticImplementationRole.RulePredicate)
        {
            return Unsupported(rule, "No resolved body is available.");
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.Content))).ToLowerInvariant();
        if (hash != body.Requirement.ContentHash)
        {
            return Unsupported(rule, "Resolved body content does not match its requirement hash.");
        }

        try
        {
            var result = provider.Evaluate(body, context);
            return result is { Satisfied: { } satisfied, Diagnostic: null }
                ? new(satisfied ? SemanticRuleStatus.Satisfied : SemanticRuleStatus.Failed, rule, rule.Message ?? $"Validation rule '{rule.Name}' failed.")
                : Unsupported(rule, result?.Diagnostic ?? "The provider supplied no predicate result.");
        }
        catch (Exception exception)
        {
            return Unsupported(rule, $"Provider threw {exception.GetType().Name}.");
        }
    }
}
