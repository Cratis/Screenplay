// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Retains producer evidence, including unknown destinations and routes.
/// </summary>
internal sealed record AuthoringProducer(
    EventSyntax Event,
    DeclarationScope Scope,
    AuthoringProducerKind Kind,
    TypeRefSyntax? Type = null,
    CommandSyntax? Command = null,
    CommandStreamSyntax? Route = null,
    bool RouteKnown = false);
