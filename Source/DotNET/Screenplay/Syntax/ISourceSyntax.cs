// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Identifies syntax that can be printed as a complete source document.
/// </summary>
public interface ISourceSyntax
{
    /// <summary>
    /// Gets the immutable options of the owning physical document.
    /// </summary>
    SourceOptions SourceOptions { get; }
}
