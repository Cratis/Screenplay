// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// An explicit import added by the timeline recipe, at an already resolved placement.
/// </summary>
internal sealed record TimelineImportPin(string DocumentPath, PlayPlacement Placement, FileImportSyntax Import);
