// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching;

internal sealed record MatchRepeatExpression(MatchExpression Body, int Minimum, int? Maximum) : MatchExpression;
