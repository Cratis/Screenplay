// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Identifies a physical model source document.
/// </summary>
/// <param name="DocumentId">The stable document identity.</param>
/// <param name="Path">The portable model document path.</param>
public sealed record ModelDocumentLocation(
    DocumentId DocumentId,
    string Path);
