// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files;

/// <summary>
/// An event-flow or read-model decision edge, or a group that suppresses its internal edges, with its diagnostic.
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
    internal bool ReadModel { get; init; }

    internal string Key
    {
        get
        {
            if (Diagnostic.Code == DiagnosticCodes.TimelineCycleGroup) return AuthoredOrder.Key([Diagnostic.Code, Container, .. Members.Order(StringComparer.Ordinal)]);
            if (ReadModel) return AuthoredOrder.Key([Diagnostic.Code, AuthoredOrder.Key(ConsumerScope), "ReadModel", Event.ToUpperInvariant()]);

            return AuthoredOrder.Key([Diagnostic.Code, AuthoredOrder.Key(ConsumerScope), Event.ToUpperInvariant()]);
        }
    }
}
