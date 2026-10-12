// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;

namespace Cratis.Screenplay.Workspaces;

sealed record WorkspacePropertyFeed(WorkspaceSyntaxEntry Entry, AuthoredDeclaration[] Events, AuthoredDeclaration[] Models, bool AutoMap, string? Nested);
