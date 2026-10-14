// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Validation;

/// <summary>
/// The settings explicitly declared by an authored gate or a command line. Null means omitted.
/// </summary>
/// <param name="Checks">Ordered selection names, aliases or all; null means omitted, empty means no checks.</param>
/// <param name="WarningsAsErrors">Whether warnings fail the run.</param>
/// <param name="Scope">The reported module, feature or slice.</param>
/// <param name="OpenQuestions">Report or fail; null means not evaluated.</param>
public sealed record DeclaredValidationGate(ImmutableArray<string>? Checks = null, bool? WarningsAsErrors = null, string? Scope = null, string? OpenQuestions = null);
