// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// An explicit reference without a local producer, shared foundation or imported event contract.
/// </summary>
/// <param name="Consumer">The referencing slice.</param>
/// <param name="Kind">The intended dependency kind.</param>
/// <param name="Role">The explicit syntax role.</param>
/// <param name="Name">The unresolved name.</param>
/// <param name="Location">The reference location.</param>
internal sealed record UnresolvedDependency(DependencyNode Consumer, string Kind, string Role, string Name, SourceLocation Location);
