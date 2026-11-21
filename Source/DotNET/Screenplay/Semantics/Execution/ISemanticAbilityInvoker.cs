// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Performs an ability invocation for a live host during reaction settlement.
/// </summary>
public interface ISemanticAbilityInvoker
{
    /// <summary>
    /// Invokes the ability with evaluated inputs. Hosts deduplicate external effects by the invocation identity.
    /// </summary>
    /// <param name="context">The invocation and its causing fact.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The returned values, outcome, failure, or a host-only pending answer.</returns>
    ValueTask<SemanticAbilityAnswer> InvokeAsync(SemanticAbilityInvocationContext context, CancellationToken cancellationToken);
}
