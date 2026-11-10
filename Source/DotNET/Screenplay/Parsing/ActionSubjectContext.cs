// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal sealed record ActionSubjectVisibility(IReadOnlyList<ScreenDataSyntax> Data, IReadOnlyList<string> Tables);

internal sealed record ActionSubjectContext(IReadOnlyList<ScreenDataSyntax> Data, DeclarationScope Scope, SourceLocation Location);
