// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.given;

/// <summary>
/// The exception that is thrown after the surfaces generated from the event-context catalog were rewritten, so a
/// regenerating run never passes.
/// </summary>
/// <param name="message">The message naming what was rewritten.</param>
public class EventContextSurfacesRegenerated(string message) : Exception(message);
