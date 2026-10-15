// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal sealed record MatchInstruction(MatchInstructionKind Kind, int Next = -1, int Alternative = -1, MatchCharacterSet? Characters = null);
