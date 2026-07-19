// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Captures;

/// <summary>
/// Defines a visitor that turns a <see cref="CaptureSyntax"/> into a consumer specific capture representation.
/// </summary>
/// <typeparam name="TCapture">The type the visitor produces.</typeparam>
public interface ICaptureSyntaxVisitor<out TCapture>
{
    /// <summary>
    /// Visits a <see cref="CaptureSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CaptureSyntax"/> to visit.</param>
    /// <returns>The consumer specific capture representation.</returns>
    TCapture Visit(CaptureSyntax syntax);
}
