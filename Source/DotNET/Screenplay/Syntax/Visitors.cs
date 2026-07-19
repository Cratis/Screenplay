// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines a visitor that turns an <see cref="ApplicationSyntax"/> into a consumer specific application representation.
/// </summary>
/// <typeparam name="TApplication">The type the visitor produces.</typeparam>
public interface IApplicationSyntaxVisitor<out TApplication>
{
    /// <summary>
    /// Visits an <see cref="ApplicationSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ApplicationSyntax"/> to visit.</param>
    /// <returns>The consumer specific application representation.</returns>
    TApplication Visit(ApplicationSyntax syntax);
}

/// <summary>
/// Defines a visitor that turns a <see cref="ModuleSyntax"/> into a consumer specific module representation.
/// </summary>
/// <typeparam name="TModule">The type the visitor produces.</typeparam>
public interface IModuleSyntaxVisitor<out TModule>
{
    /// <summary>
    /// Visits a <see cref="ModuleSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ModuleSyntax"/> to visit.</param>
    /// <returns>The consumer specific module representation.</returns>
    TModule Visit(ModuleSyntax syntax);
}

/// <summary>
/// Defines a visitor that turns a <see cref="FeatureSyntax"/> into a consumer specific feature representation.
/// </summary>
/// <typeparam name="TFeature">The type the visitor produces.</typeparam>
public interface IFeatureSyntaxVisitor<out TFeature>
{
    /// <summary>
    /// Visits a <see cref="FeatureSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="FeatureSyntax"/> to visit.</param>
    /// <returns>The consumer specific feature representation.</returns>
    TFeature Visit(FeatureSyntax syntax);
}

/// <summary>
/// Defines a visitor that turns a <see cref="SliceSyntax"/> into a consumer specific slice representation.
/// </summary>
/// <typeparam name="TSlice">The type the visitor produces.</typeparam>
public interface ISliceSyntaxVisitor<out TSlice>
{
    /// <summary>
    /// Visits a <see cref="SliceSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SliceSyntax"/> to visit.</param>
    /// <returns>The consumer specific slice representation.</returns>
    TSlice Visit(SliceSyntax syntax);
}

/// <summary>
/// Defines a visitor that turns a <see cref="ConstraintSyntax"/> into a consumer specific constraint representation.
/// </summary>
/// <typeparam name="TConstraint">The type the visitor produces.</typeparam>
public interface IConstraintSyntaxVisitor<out TConstraint>
{
    /// <summary>
    /// Visits a <see cref="ConstraintSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ConstraintSyntax"/> to visit.</param>
    /// <returns>The consumer specific constraint representation.</returns>
    TConstraint Visit(ConstraintSyntax syntax);
}
