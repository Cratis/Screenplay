// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files;

/// <summary>
/// An event-flow edge, or a group that suppresses its internal edges, together with its unchanged diagnostic.
/// </summary>
internal sealed record TimelineFinding(
    Diagnostic Diagnostic,
    string[] ConsumerScope,
    string[] ProducerScope,
    string Event,
    string Container,
    string Left,
    string Right,
    bool OwnSubFeature,
    string[] Members)
{
    internal string Key => Diagnostic.Code == DiagnosticCodes.TimelineCycleGroup
        ? AuthoredOrder.Key([Diagnostic.Code, Container, .. Members.Order(StringComparer.Ordinal)])
        : AuthoredOrder.Key([Diagnostic.Code, AuthoredOrder.Key(ConsumerScope), Event.ToUpperInvariant()]);
}
