// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents one addressable <c>$eventContext.&lt;path&gt;</c> the <see cref="EventContextCatalog"/> lists.
/// </summary>
/// <param name="Path">The dotted path as written after <c>$eventContext.</c>, such as <c>eventType.id</c>.</param>
/// <param name="Member">The <see cref="EventContextMember"/> the last segment of the path names.</param>
public record EventContextPath(string Path, EventContextMember Member);
