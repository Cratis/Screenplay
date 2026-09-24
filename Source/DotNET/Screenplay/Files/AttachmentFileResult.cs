// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files;

/// <summary>Contents and warnings for implementation attachments loaded from a physical root.</summary>
public sealed record AttachmentFileResult
{
    /// <summary>Gets text keyed by normalized repository-relative path.</summary>
    public required ImmutableDictionary<string, string> Contents { get; init; }

    /// <summary>Gets warnings for attachments that could not be loaded.</summary>
    public required ImmutableArray<Diagnostic> Diagnostics { get; init; }
}
