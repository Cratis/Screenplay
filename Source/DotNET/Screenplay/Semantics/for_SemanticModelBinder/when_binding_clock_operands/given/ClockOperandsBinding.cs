// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands.given;

/// <summary>The bound application and the versions it selected, as the clock specs read them.</summary>
/// <param name="Application">The bound application.</param>
/// <param name="LanguageVersion">The selected language version.</param>
/// <param name="SemanticVersion">The selected semantic version.</param>
public sealed record ClockOperandsBinding(SemanticApplication Application, LanguageVersion LanguageVersion, SemanticVersion SemanticVersion);
