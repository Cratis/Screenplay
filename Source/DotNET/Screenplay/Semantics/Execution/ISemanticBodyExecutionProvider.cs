// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies host-owned execution of named rule predicates, code validations and reducer transitions. Screenplay does not compile or run source code.
/// </summary>
/// <remarks>
/// The host is responsible for language admission, isolation and resource limits. The pure capability on a
/// requirement is a request, not a sandbox guarantee. A provider must return Unsupported for bodies it cannot safely execute.
/// </remarks>
public interface ISemanticBodyExecutionProvider : IAsyncDisposable
{
    /// <summary>Evaluates a named rule against portable semantic values.</summary>
    /// <param name="body">The resolved, content-verified body and its requirement.</param>
    /// <param name="context">The rule value, artifact and caller-supplied circumstances; no roles or claims.</param>
    /// <param name="cancellationToken">The run cancellation; the host must also enforce execution time limits.</param>
    /// <returns>A Boolean answer, or a diagnostic with no answer when execution is unsupported.</returns>
    /// <remarks>Timeouts and cancellation, including OperationCanceledException, map to Unsupported, never rejection.</remarks>
    ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken);

    /// <summary>Executes a command or concept code validation against portable semantic values.</summary>
    /// <param name="body">The resolved, content-verified body and its requirement.</param>
    /// <param name="context">The whole command or concept value and caller-supplied circumstances.</param>
    /// <param name="cancellationToken">The run cancellation; the host must also enforce execution time limits.</param>
    /// <returns>Validation messages, or an unsupported result when execution is unavailable.</returns>
    /// <remarks>Exceptions and cancellation map to Unsupported, never rejection. Existing providers remain unsupported by default.</remarks>
    ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) =>
        new(SemanticValidationBodyResult.Unsupported(SemanticBodyExecutionDefaults.NotImplemented("Code validation", body)));

    /// <summary>
    /// Evaluates a policy predicate against the supplied semantic artifact and caller.
    /// </summary>
    /// <param name="body">The resolved, content-verified policy implementation.</param>
    /// <param name="context">The artifact, caller and known circumstances.</param>
    /// <param name="cancellationToken">Run cancellation; hosts must also enforce time limits.</param>
    /// <returns>An allow, deny, or unsupported answer. Existing providers remain unsupported by default.</returns>
    ValueTask<SemanticPolicyBodyResult> AuthorizeAsync(SemanticImplementationBody body, SemanticPolicyContext context, CancellationToken cancellationToken) =>
        new(SemanticPolicyBodyResult.Unsupported(SemanticBodyExecutionDefaults.NotImplemented("Policy", body)));

    /// <summary>
    /// Executes an attached, constrained result-v1 command handler.
    /// </summary>
    /// <param name="body">The resolved, content-verified implementation.</param>
    /// <param name="context">The command and known circumstances, without invented values.</param>
    /// <param name="cancellationToken">Run cancellation; hosts must also enforce time limits.</param>
    /// <returns>Ordered event payloads, or unsupported for admission refusal. Existing providers remain unsupported by default.</returns>
    /// <remarks>
    /// Admitted handler faults propagate as <see cref="SemanticCommandBodyFaultException"/> before any events are produced or committed.
    /// Hosts may report time or memory limits with its typed kind. Other exceptions become Faulted; unrequested cancellation becomes Cancelled.
    /// Cancellation requested through the supplied token propagates as the original OperationCanceledException. Malformed payloads remain unsupported.
    /// These fault semantics apply only to command handlers, not other provider roles.
    /// </remarks>
    ValueTask<SemanticCommandBodyResult> HandleAsync(SemanticImplementationBody body, SemanticCommandContext context, CancellationToken cancellationToken) =>
        new(SemanticCommandBodyResult.Unsupported(SemanticBodyExecutionDefaults.NotImplemented("Command handler", body)));

    /// <summary>
    /// Runs a standalone event-trigger reaction body after its where guard passed.
    /// </summary>
    /// <param name="body">The resolved, content-verified result-version-1 reaction body.</param>
    /// <param name="context">The typed event context without read access.</param>
    /// <param name="cancellationToken">Cancellation for this run.</param>
    /// <returns>Empty, events or commands; the whole batch is validated before settlement.</returns>
    /// <remarks>
    /// Missing results, faults, timeouts and non-run cancellation are Unsupported with Reaction capability.
    /// Cancellation of the run itself propagates. Existing providers default to Unsupported.
    /// </remarks>
    ValueTask<SemanticReactionBodyResult> ReactAsync(SemanticImplementationBody body, SemanticReactionContext context, CancellationToken cancellationToken) =>
        new(SemanticReactionBodyResult.Unsupported(SemanticBodyExecutionDefaults.NotImplemented("Reaction effect", body)));

    /// <summary>Executes one reducer transition against its portable context.</summary>
    /// <param name="body">The resolved, content-verified transition body.</param>
    /// <param name="context">The prior state, event and caller-supplied circumstances.</param>
    /// <param name="cancellationToken">Run cancellation; hosts must enforce time limits as well.</param>
    /// <returns>The next state, an explicit deletion, or an unsupported diagnostic.</returns>
    /// <remarks>Exceptions and cancellation map to Unsupported. Existing providers remain unsupported by default.</remarks>
    ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken) =>
        new(SemanticReducerBodyResult.Unsupported(SemanticBodyExecutionDefaults.NotImplemented("Reducer transition", body)));
}

/// <summary>Represents resolved source for one implementation requirement.</summary>
/// <param name="Requirement">The language, identity, hash and source mapping contract.</param>
/// <param name="Content">The exact decoded body content whose UTF-8 hash matches the requirement.</param>
public sealed record SemanticImplementationBody(SemanticImplementationRequirement Requirement, string Content);

/// <summary>Represents a named predicate's answer, not execution of a validate iterator.</summary>
/// <param name="Satisfied">True or false for an evaluated predicate; null means unsupported.</param>
/// <param name="Diagnostic">An execution/admission diagnostic. A diagnostic always makes the result unsupported.</param>
public sealed record SemanticBodyExecutionResult(bool? Satisfied, string? Diagnostic = null);
