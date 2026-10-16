// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// The kind of template a <see cref="TemplateDescriptor"/> describes.
/// </summary>
public enum TemplateDescriptorKind
{
    /// <summary>
    /// A layout - the outermost template, and what blueprints ship as shells.
    /// </summary>
    Layout,

    /// <summary>
    /// A screen template.
    /// </summary>
    ScreenTemplate,

    /// <summary>
    /// A dialog template.
    /// </summary>
    DialogTemplate
}

/// <summary>
/// Describes one template of a blueprint, or of the document, as far as the application shell rules need to know it. Mirrors
/// the facts Scene's <c>TemplateApplicability</c> and <c>ShellResolver</c> decide by.
/// </summary>
/// <param name="Name">The template's name.</param>
/// <param name="Kind">What kind of template it is.</param>
public record TemplateDescriptor(string Name, TemplateDescriptorKind Kind)
{
    /// <summary>
    /// Gets a value indicating whether the screen template has the root role - it may be the application shell.
    /// </summary>
    public bool IsRoot { get; init; }

    /// <summary>
    /// Gets the scopes the template declares (<c>application</c>, <c>module</c>, <c>feature</c>, <c>subfeature</c>, <c>slice</c>), or <see langword="null"/> when it declares none and the structural role decides.
    /// </summary>
    public IReadOnlyList<string>? Scopes { get; init; }

    /// <summary>
    /// Gets the template's <c>type</c>, or <see langword="null"/>.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets a value indicating whether the screen template fits a slot of another template.
    /// </summary>
    public bool FitsSlot { get; init; }

    /// <summary>
    /// Gets the names of the slots (outlets) the template declares.
    /// </summary>
    public IReadOnlyList<string> Slots { get; init; } = [];

    /// <summary>
    /// Gets the slot every screen's content is placed in.
    /// </summary>
    public string PrimaryOutlet { get; init; } = "content";

    /// <summary>
    /// Gets the names of the options the template exposes, or <see langword="null"/> when they are not known and are not checked.
    /// </summary>
    public IReadOnlyList<string>? Options { get; init; }
}

/// <summary>
/// Describes an installed blueprint: its name, installed version and the templates it ships.
/// </summary>
/// <param name="Name">The blueprint package's name.</param>
/// <param name="Version">The installed version.</param>
/// <param name="Templates">The templates the blueprint ships.</param>
public record BlueprintDescriptor(string Name, string Version, IReadOnlyList<TemplateDescriptor> Templates);
