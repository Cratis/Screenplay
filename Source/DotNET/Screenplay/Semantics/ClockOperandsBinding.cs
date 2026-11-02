// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// A candidate graph, not an executable model of a published ESM version: the working major 37 is neither registered nor
// serializable. Published ESM 36 does not admit application calendars or clock operands.
internal sealed record ClockOperandsBinding(SemanticApplication Application, LanguageVersion LanguageVersion, SemanticVersion SemanticVersion);
