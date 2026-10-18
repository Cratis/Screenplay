// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Deliberately separate from the normalized execution AST: occurrence identity and empty
// alternatives must survive here, including alternatives the matcher deduplicates.
internal sealed record MatchAmbiguityNode(string Kind, int Offset, int Length, MatchAmbiguityNode[] Children, int Minimum = 0, int? Maximum = 0, MatchCharacterSet? Characters = null);
