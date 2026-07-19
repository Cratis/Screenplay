// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Defines a visitor that turns a <see cref="SpecificationSyntax"/> into a consumer specific specification representation.
/// </summary>
/// <typeparam name="TSpecification">The type the visitor produces.</typeparam>
public interface ISpecificationSyntaxVisitor<out TSpecification>
{
    /// <summary>
    /// Visits a <see cref="SpecificationSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationSyntax"/> to visit.</param>
    /// <returns>The consumer specific specification representation.</returns>
    TSpecification Visit(SpecificationSyntax syntax);
}
