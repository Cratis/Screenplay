// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Compares authored model structure without claiming execution or behavioral equivalence.
/// </summary>
public static class ModelComparison
{
    /// <summary>
    /// Compares persisted identities within one application, or exact addresses when either input has no persisted identities.
    /// </summary>
    /// <param name="before">The baseline authored model.</param>
    /// <param name="after">The candidate authored model.</param>
    /// <returns>Typed structural changes and explicit comparison coverage gaps.</returns>
    /// <exception cref="IncompatibleModelIdentities">Both inputs declare persisted identities but belong to different applications.</exception>
    public static ModelDifference Compare(ComparedModel before, ComparedModel after)
    {
        var matching = before.HasPersistedIdentities && after.HasPersistedIdentities ? DeclarationMatching.Identity : DeclarationMatching.Address;
        if (matching == DeclarationMatching.Identity && before.Workspace.IdentityCatalog.Application != after.Workspace.IdentityCatalog.Application)
        {
            throw new IncompatibleModelIdentities("Models with persisted identities must belong to the same application identity; identity continuity cannot be inferred across applications.");
        }

        var difference = StructuralComparison.Compare(before.Workspace, after.Workspace, matchByAddress: matching == DeclarationMatching.Address);

        return ModelDifferenceProjection.Project(difference, matching, before.Workspace.Compilation.Success, after.Workspace.Compilation.Success);
    }
}
