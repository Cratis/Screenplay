// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Printing;

/// <summary>
/// The exception that is thrown when the printer meets a syntax node or kind it has no surface form for.
/// </summary>
/// <remarks>
/// Printing an unknown node as an empty string, a guessed default or its bare operand would produce plausible
/// but wrong source, so the printer fails instead. It derives from <see cref="InvalidOperationException"/> so
/// authoring hosts that already fail an edit on an invalid operation report it as a failed edit rather than
/// a crash.
/// </remarks>
/// <param name="construct">What was being printed, for example "expression" or "validation rule kind".</param>
/// <param name="kind">The node type name or kind value that has no surface form.</param>
public sealed class UnsupportedSyntaxForPrinting(string construct, string kind)
    : InvalidOperationException($"The Screenplay printer has no surface form for {construct} '{kind}'. Teach the printer to write it before it can be printed.");
