// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// A candidate graph, not an executable model: v35 validation and canonical serialization join in later slices.
internal sealed record PossibleEventsBinding(SemanticApplication Application, LanguageVersion LanguageVersion, SemanticVersion SemanticVersion);
