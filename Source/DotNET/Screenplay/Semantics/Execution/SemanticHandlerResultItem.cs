// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed record SemanticHandlerResultItem(SemanticProducedEvent Entry, SemanticValue Payload, SemanticValue? Destination);
