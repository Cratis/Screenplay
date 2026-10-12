// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

// One physical mapping can have several effective consumers: feeds, inherited handlers and variants.
// Retain each consumer, including incomplete explicit mappings, independently of executable admission.
sealed record WorkspacePropertyProjectionEffect(
    WorkspaceSyntaxEntry Feed,
    AuthoredDeclaration[] Events,
    AuthoredDeclaration[] Targets,
    MappingSyntax Mapping,
    WorkspacePropertyProjectionSource[] Sources,
    bool Complete);

sealed record WorkspacePropertyProjectionSource(string Path, AuthoredDeclaration[] Members);
