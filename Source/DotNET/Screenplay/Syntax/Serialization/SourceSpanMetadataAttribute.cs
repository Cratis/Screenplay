// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// Marks a syntax property as parser-owned source-span metadata, excluded from the typed JSON codec, the syntax
/// schema, and structural comparison.
/// </summary>
/// <remarks>
/// Every <see cref="Diagnostics.SourceLocation"/>-typed property is metadata already; this marks the companion
/// raw lengths, which are plain integers and cannot be recognized by type.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
internal sealed class SourceSpanMetadataAttribute : Attribute;
