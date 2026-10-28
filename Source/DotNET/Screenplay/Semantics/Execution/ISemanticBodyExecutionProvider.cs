// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies host-owned execution of named rule predicates. Screenplay does not compile or run source code.
/// </summary>
/// <remarks>
/// The host is responsible for language admission, isolation and resource limits. The pure capability on a
/// requirement is a request, not a sandbox guarantee. A provider must return Unsupported for bodies it cannot safely execute.
/// </remarks>
public interface ISemanticBodyExecutionProvider
{
    /// <summary>Evaluates a named rule against portable semantic values.</summary>
    /// <param name="body">The resolved, content-verified body and its requirement.</param>
    /// <param name="context">The rule value, artifact and caller-supplied circumstances; no roles or claims.</param>
    /// <returns>A Boolean answer, or a diagnostic with no answer when execution is unsupported.</returns>
    SemanticBodyExecutionResult Evaluate(SemanticImplementationBody body, SemanticRuleContext context);
}

/// <summary>Represents resolved source for one implementation requirement.</summary>
/// <param name="Requirement">The language, identity, hash and source mapping contract.</param>
/// <param name="Content">The exact decoded body content whose UTF-8 hash matches the requirement.</param>
public sealed record SemanticImplementationBody(SemanticImplementationRequirement Requirement, string Content);

/// <summary>Represents a named predicate's answer, not execution of a validate iterator.</summary>
/// <param name="Satisfied">True or false for an evaluated predicate; null means unsupported.</param>
/// <param name="Diagnostic">An execution/admission diagnostic. A diagnostic always makes the result unsupported.</param>
public sealed record SemanticBodyExecutionResult(bool? Satisfied, string? Diagnostic = null);
