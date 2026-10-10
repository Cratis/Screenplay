// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Reports one direct indexed dependant, not transitive or runtime impact.
/// </summary>
/// <param name="Changed">The declaration that changed.</param>
/// <param name="Side">The index snapshot containing the dependant.</param>
/// <param name="DependantAddress">The dotted dependant address.</param>
/// <param name="Role">The indexed reference role.</param>
/// <param name="Resolution">The index resolution confidence.</param>
public sealed record DirectDependant(
    ComparedDeclaration Changed,
    ComparedSide Side,
    string DependantAddress,
    string Role,
    DependantResolution Resolution);
