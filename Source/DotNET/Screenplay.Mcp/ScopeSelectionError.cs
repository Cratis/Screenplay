// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Represents an anticipated failure to select exactly one module, feature or slice.
/// </summary>
/// <param name="Kind">The reason selection failed.</param>
/// <param name="Message">The explanation suitable for reporting to the caller.</param>
public sealed record ScopeSelectionError(ScopeSelectionErrorKind Kind, string Message);
