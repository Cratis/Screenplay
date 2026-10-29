// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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

    internal static bool MatchesHash(SemanticImplementationBody body)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.Content))).ToLowerInvariant();
        return hash == body.Requirement.ContentHash;
    }

    internal SemanticImplementationBody? FindBody(string? id, SemanticImplementationRole role)
    {
        if (id is null || !_bodies.TryGetValue(id, out var body)) return null;
        return body.Requirement.Role == role ? body : null;
    }

    internal SemanticRuleCheck Check(SemanticValidationRule rule, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        if (FindBody(rule.RequirementId, SemanticImplementationRole.RulePredicate) is not { } body)
        {
            return Unsupported(rule, "No resolved body is available.");
        }

        if (!MatchesHash(body))
        {
            return Unsupported(rule, "Resolved body content does not match its requirement hash.");
        }

        return Evaluate(rule, body, context, cancellationToken);
    }

    internal ImmutableArray<SemanticRuleCheck> CheckValidation(SemanticValidationRule rule, SemanticImplementationRole role, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        if (FindBody(rule.RequirementId, role) is not { } body)
        {
            return [Unsupported(rule, "No resolved body is available.")];
        }

        if (!MatchesHash(body))
        {
            return [Unsupported(rule, "Resolved body content does not match its requirement hash.")];
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Task.Run(async () => await provider.ValidateAsync(body, context, cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            return ValidationAnswer(rule, result);
        }
        catch (Exception exception)
        {
            return [Unsupported(rule, $"Provider threw {exception.GetType().Name}.")];
        }
    }

    static ImmutableArray<SemanticRuleCheck> ValidationAnswer(SemanticValidationRule rule, SemanticValidationBodyResult? result)
    {
        if (result?.Diagnostic is { } diagnostic) return [Unsupported(rule, diagnostic)];
        if (result?.Messages is not { IsDefault: false } messages || messages.Any(message => message is null))
        {
            return [Unsupported(rule, "The provider supplied no validation result.")];
        }

        return [.. messages.Select(message => new SemanticRuleCheck(SemanticRuleStatus.Failed, rule, message))];
    }

    static SemanticRuleCheck Answer(SemanticValidationRule rule, SemanticBodyExecutionResult? result) =>
        result is { Satisfied: { } satisfied, Diagnostic: null }
            ? new(satisfied ? SemanticRuleStatus.Satisfied : SemanticRuleStatus.Failed, rule, rule.Message ?? $"Validation rule '{rule.Name}' failed.")
            : Unsupported(rule, result?.Diagnostic ?? "The provider supplied no predicate result.");

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
