// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

sealed record StructuralDifference(StructuralComparison.Change[] Changes, StructuralSection[] Sections, bool Complete, bool? HasSemanticChange, string[] Limits, int BeforeDeclarations, int AfterDeclarations);
