// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Identifies why a scope could not be selected.
/// </summary>
public enum ScopeSelectionErrorKind
{
    /// <summary>
    /// No module, feature or slice has the requested case-sensitive address.
    /// </summary>
    UnknownScope,

    /// <summary>
    /// More than one module, feature or slice has the requested address.
    /// </summary>
    AmbiguousScope,

    /// <summary>
    /// The path does not exist or is a file without the .play extension.
    /// </summary>
    InvalidPath,

    /// <summary>
    /// Reading the application failed because of an I/O or permission error.
    /// </summary>
    UnreadablePath
}
