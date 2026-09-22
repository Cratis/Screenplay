// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The exception that is thrown when typed source authoring is structurally invalid.
/// </summary>
/// <param name="message">The rejection reason.</param>
public sealed class InvalidWorkspaceAuthoring(string message) : Exception(message);
