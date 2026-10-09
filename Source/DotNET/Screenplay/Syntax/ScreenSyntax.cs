// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents what a toolbar item activates.
/// </summary>
public enum ToolbarItemKind
{
    /// <summary>
    /// The item target is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The item executes a command or behavior.
    /// </summary>
    Action = 1,

    /// <summary>
    /// The item navigates to a screen.
    /// </summary>
    Navigate = 2,

    /// <summary>
    /// The item opens a dialog template.
    /// </summary>
    Dialog = 3
}

/// <summary>
/// Represents a <c>screen</c> declaration - the user interface of a slice.
/// </summary>
/// <param name="Name">The name of the screen.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the screen lives in an external file.</param>
/// <param name="Directives">The <see cref="ScreenDirectiveSyntax">directives</see> making up the screen body.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSyntax(
    string Name,
    FileReferenceSyntax? File,
    IEnumerable<ScreenDirectiveSyntax> Directives,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the report-only description, distinct from the UI title.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents the base of every directive in a screen body.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ScreenDirectiveSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>data</c> directive binding a read model through a query.
/// </summary>
/// <param name="Type">The <see cref="TypeRefSyntax"/> of the bound read model.</param>
/// <param name="Query">The name of the query providing the data.</param>
/// <param name="By">The optional parameter the query is keyed by.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenDataSyntax(TypeRefSyntax Type, string Query, string? By, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents an <c>action</c> directive exposing a command on the screen.
/// </summary>
/// <param name="Command">The name of the command the action invokes.</param>
/// <param name="Label">The optional display label.</param>
/// <param name="Navigate">The optional <see cref="ScreenNavigateSyntax"/> performed after the action.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenActionSyntax(string Command, string? Label, ScreenNavigateSyntax? Navigate, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>navigate to</c> directive.
/// </summary>
/// <param name="Screen">The name of the target screen.</param>
/// <param name="By">The optional parameter carried to the target screen.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenNavigateSyntax(string Screen, string? By, SourceLocation Location) : ScreenDirectiveSyntax(Location)
{
    /// <summary>
    /// Gets the explicit route override, or <c>null</c> when navigation uses the target screen's default route.
    /// </summary>
    public string? Route { get; init; }

    /// <summary>
    /// Gets the route or navigation parameters supplied by the author.
    /// </summary>
    public IEnumerable<ScreenNavigationParameterSyntax> Parameters { get; init; } = [];
}

/// <summary>
/// Represents a <c>template</c> reference filling the named slots of a declared <see cref="ScreenTemplateSyntax"/>
/// or <see cref="DialogTemplateSyntax"/>.
/// </summary>
/// <param name="Name">The name of the referenced template.</param>
/// <param name="Slots">The <see cref="ScreenSlotSyntax">slots</see> being filled.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A screen is an instance: it names the structure it fills and provides the content. The structure it names
/// is a template - the application's <see cref="LayoutSyntax"/> is selected once by a <see cref="UiProfileSyntax"/>
/// rather than named per screen.
/// </remarks>
public record ScreenTemplateReferenceSyntax(string Name, IEnumerable<ScreenSlotSyntax> Slots, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a named slot within a template reference.
/// </summary>
/// <param name="Name">The name of the slot.</param>
/// <param name="Directives">The <see cref="ScreenDirectiveSyntax">directives</see> filling the slot.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSlotSyntax(string Name, IEnumerable<ScreenDirectiveSyntax> Directives, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>section</c> directive grouping related directives.
/// </summary>
/// <param name="Name">The name of the section.</param>
/// <param name="Directives">The <see cref="ScreenDirectiveSyntax">directives</see> in the section.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSectionSyntax(string Name, IEnumerable<ScreenDirectiveSyntax> Directives, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>title</c> directive.
/// </summary>
/// <param name="Text">The title text.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenTitleSyntax(string Text, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>table</c> widget.
/// </summary>
/// <param name="Target">The collection or read model the table shows.</param>
/// <param name="Columns">The <see cref="ScreenColumnSyntax">columns</see> of the table.</param>
/// <param name="RowClick">The optional <see cref="ScreenNavigateSyntax"/> performed on row click.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenTableSyntax(
    string Target,
    IEnumerable<ScreenColumnSyntax> Columns,
    ScreenNavigateSyntax? RowClick,
    SourceLocation Location) : ScreenDirectiveSyntax(Location)
{
    /// <summary>
    /// Gets the behaviors attached inline to the table.
    /// </summary>
    public IEnumerable<BehaviorSyntax> Behaviors { get; init; } = [];

    /// <summary>
    /// Gets the named behaviors attached to the table with <c>uses</c>.
    /// </summary>
    public IEnumerable<UsesBehaviorSyntax> UsedBehaviors { get; init; } = [];
}

/// <summary>
/// Represents a column of a <c>table</c> widget.
/// </summary>
/// <param name="Property">The property shown in the column.</param>
/// <param name="Label">The optional display label.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenColumnSyntax(string Property, string? Label, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>summary</c> widget.
/// </summary>
/// <param name="Target">The read model the summary shows.</param>
/// <param name="Fields">The <see cref="ScreenFieldSyntax">fields</see> of the summary.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenSummarySyntax(string Target, IEnumerable<ScreenFieldSyntax> Fields, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a field of a <c>summary</c> widget.
/// </summary>
/// <param name="Property">The property shown in the field.</param>
/// <param name="Label">The display label.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenFieldSyntax(string Property, string Label, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an inline code block within a screen body.
/// </summary>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> holding the code.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenCodeSyntax(CodeBlockSyntax Code, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a behavior attached where it was written - an inline <c>on</c> block, which is an anonymous
/// <see cref="BehaviorSyntax"/>.
/// </summary>
/// <param name="Behavior">The attached behavior.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the attachment.</param>
public record ScreenBehaviorSyntax(BehaviorSyntax Behavior, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a named behavior attached with <c>uses</c>.
/// </summary>
/// <param name="Uses">The attachment.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the attachment.</param>
public record ScreenUsesBehaviorSyntax(UsesBehaviorSyntax Uses, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents a <c>component &lt;Component&gt; &lt;Name&gt;</c> directive that instantiates a package component.
/// </summary>
/// <param name="Component">The dotted package component identity.</param>
/// <param name="Name">The stable authored instance name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenComponentSyntax(string Component, string Name, SourceLocation Location) : ScreenDirectiveSyntax(Location)
{
    /// <summary>
    /// Gets the exact stable id used by renderers and component bindings, or <c>null</c> when <see cref="Name"/> is the stable id.
    /// </summary>
    public string? StableId { get; init; }

    /// <summary>
    /// Gets the data context binding for the component.
    /// </summary>
    public UiBindingSyntax? Context { get; init; }

    /// <summary>
    /// Gets authored property bindings and literal values.
    /// </summary>
    public IEnumerable<ComponentPropertySyntax> Properties { get; init; } = [];

    /// <summary>
    /// Gets values exposed by this component instance to surrounding bindings.
    /// </summary>
    public IEnumerable<ComponentExposedValueSyntax> Exposes { get; init; } = [];

    /// <summary>
    /// Gets package-specific presentation hints.
    /// </summary>
    public IEnumerable<PresentationValueSyntax> Presentation { get; init; } = [];

    /// <summary>
    /// Gets the icon associated with the component instance.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets recursive outlets authored inside the component.
    /// </summary>
    public IEnumerable<ComponentOutletSyntax> Outlets { get; init; } = [];

    /// <summary>
    /// Gets interactions attached to the component instance.
    /// </summary>
    public IEnumerable<BehaviorSyntax> Behaviors { get; init; } = [];

    /// <summary>
    /// Gets named behaviors attached to the component instance.
    /// </summary>
    public IEnumerable<UsesBehaviorSyntax> UsedBehaviors { get; init; } = [];
}

/// <summary>
/// Represents a component property value, either bound from data or supplied as a literal.
/// </summary>
/// <param name="Property">The component property path.</param>
/// <param name="Binding">The binding expression, or <c>null</c> when the value is literal.</param>
/// <param name="Value">The literal value, or <c>null</c> when the property is bound.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ComponentPropertySyntax(string Property, UiBindingSyntax? Binding, ExpressionSyntax? Value, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an exposed component instance value.
/// </summary>
/// <param name="Name">The exposed value name.</param>
/// <param name="Binding">The binding expression that supplies the value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ComponentExposedValueSyntax(string Name, UiBindingSyntax Binding, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a presentation hint for a component or toolbar item.
/// </summary>
/// <param name="Name">The presentation key.</param>
/// <param name="Value">The authored value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PresentationValueSyntax(string Name, string Value, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a recursive outlet nested inside a component.
/// </summary>
/// <param name="Name">The outlet name.</param>
/// <param name="Directives">The screen directives rendered inside the outlet.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ComponentOutletSyntax(string Name, IEnumerable<ScreenDirectiveSyntax> Directives, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>toolbar</c> directive.
/// </summary>
/// <param name="Name">The toolbar name.</param>
/// <param name="Items">The toolbar items.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenToolbarSyntax(string Name, IEnumerable<ToolbarItemSyntax> Items, SourceLocation Location) : ScreenDirectiveSyntax(Location);

/// <summary>
/// Represents one toolbar item.
/// </summary>
/// <param name="Name">The stable item name.</param>
/// <param name="Kind">The kind of target.</param>
/// <param name="Target">The target command, behavior, screen or dialog template.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ToolbarItemSyntax(string Name, ToolbarItemKind Kind, string Target, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the item label.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets the icon shown for the item.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets navigation or action parameters supplied by the item.
    /// </summary>
    public IEnumerable<ScreenNavigationParameterSyntax> Parameters { get; init; } = [];

    /// <summary>
    /// Gets package-specific presentation hints.
    /// </summary>
    public IEnumerable<PresentationValueSyntax> Presentation { get; init; } = [];
}

/// <summary>
/// Represents a navigation or toolbar parameter.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Binding">The binding expression.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenNavigationParameterSyntax(string Name, UiBindingSyntax Binding, SourceLocation Location) : SyntaxNode(Location);
