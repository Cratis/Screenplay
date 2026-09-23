// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents what ESM v1 makes of one <c>$eventContext.&lt;path&gt;</c> read.
/// </summary>
/// <param name="Path">The canonical path when the catalog knows it, otherwise the path as written.</param>
/// <param name="Kind">The <see cref="SemanticEventContextScalarKind"/>.</param>
/// <param name="Primitive">The portable primitive a <see cref="SemanticEventContextScalarKind.Scalar"/> carries.</param>
/// <param name="Reason">Why a <see cref="SemanticEventContextScalarKind.Rejected"/> path is not admitted.</param>
readonly record struct SemanticEventContextScalar(string Path, SemanticEventContextScalarKind Kind, SemanticPrimitiveType Primitive, string Reason)
{
    /// <summary>
    /// Gets a value indicating whether the path is admitted.
    /// </summary>
    public bool IsAdmitted => Kind != SemanticEventContextScalarKind.Rejected;

    /// <summary>
    /// Creates a rejected read.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="reason">Why it is not admitted.</param>
    /// <returns>The rejected <see cref="SemanticEventContextScalar"/>.</returns>
    public static SemanticEventContextScalar Rejected(string path, string reason) =>
        new(path, SemanticEventContextScalarKind.Rejected, SemanticPrimitiveType.Unknown, reason);
}
