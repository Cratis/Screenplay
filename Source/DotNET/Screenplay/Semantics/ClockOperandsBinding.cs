// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// A candidate graph, not an executable model of a published ESM version: the working major 36 is neither registered nor
// serializable until the application calendar and clock operands are admitted after ESM 35 publishes.
internal sealed record ClockOperandsBinding(SemanticApplication Application, LanguageVersion LanguageVersion, SemanticVersion SemanticVersion);
