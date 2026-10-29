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
        new(SemanticRuleStatus.NotEvaluable, rule, $"Rule body (requirement {rule.RequirementId}): {Sanitize(diagnostic)}");

    internal static string Sanitize(string diagnostic) => string.Concat(diagnostic.Where(character => !char.IsControl(character)).Take(500));

    internal SemanticRuleCheck Check(SemanticValidationRule rule, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        if (FindBody(rule) is not { } body)
        {
            return Unsupported(rule, "No resolved body is available.");
        }

        if (!MatchesHash(body))
        {
            return Unsupported(rule, "Resolved body content does not match its requirement hash.");
        }

        return Evaluate(rule, body, context, cancellationToken);
    }

    static SemanticRuleCheck Answer(SemanticValidationRule rule, SemanticBodyExecutionResult? result) =>
        result is { Satisfied: { } satisfied, Diagnostic: null }
            ? new(satisfied ? SemanticRuleStatus.Satisfied : SemanticRuleStatus.Failed, rule, rule.Message ?? $"Validation rule '{rule.Name}' failed.")
            : Unsupported(rule, result?.Diagnostic ?? "The provider supplied no predicate result.");

    static bool MatchesHash(SemanticImplementationBody body)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.Content))).ToLowerInvariant();
        return hash == body.Requirement.ContentHash;
    }

    SemanticImplementationBody? FindBody(SemanticValidationRule rule)
    {
        if (rule.RequirementId is not { } id) return null;
        if (!_bodies.TryGetValue(id, out var body)) return null;
        return body.Requirement.Role == SemanticImplementationRole.RulePredicate ? body : null;
    }

    SemanticRuleCheck Evaluate(SemanticValidationRule rule, SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The reference evaluator is synchronous. Invoke the asynchronous host outside its synchronization context.
            var result = Task.Run(async () => await provider.EvaluateAsync(body, context, cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            return Answer(rule, result);
        }
        catch (Exception exception)
        {
            return Unsupported(rule, $"Provider threw {exception.GetType().Name}.");
        }
    }
}
