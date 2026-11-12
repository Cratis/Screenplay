// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions;

/// <summary>Says how far a setting's value travels.</summary>
public enum ExtensionSettingExposure
{
    /// <summary>The value stays with the backend.</summary>
    Backend,

    /// <summary>The value may reach the browser.</summary>
    Public,

    /// <summary>The value comes from a secret store and never reaches the browser.</summary>
    Secret
}

/// <summary>
/// Represents the parsed <c>contract.play</c> of an extension package: its manifest and everything it contributes.
/// </summary>
/// <remarks>
/// The contract is the only part of a package the compiler reads. A <c>system</c> that names the package gets the
/// abilities, triggers and components as if the author had declared them.
/// </remarks>
/// <param name="Name">The extension name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the declaration starts.</param>
public record ExtensionContract(string Name, SourceLocation Location)
{
    /// <summary>Gets the manifest display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the manifest description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the package-relative path of the icon that represents the package.</summary>
    public string? Icon { get; init; }

    /// <summary>Gets the marketplace images.</summary>
    public ImmutableArray<ExtensionImage> Images { get; init; } = [];

    /// <summary>Gets the compatibility requirements.</summary>
    public ImmutableArray<ExtensionRequirement> Requirements { get; init; } = [];

    /// <summary>Gets the default activation of the backend.</summary>
    public SystemActivationSyntax? Activation { get; init; }

    /// <summary>Gets the settings the extension declares.</summary>
    public ImmutableArray<ExtensionSetting> Settings { get; init; } = [];

    /// <summary>Gets the abilities the extension offers.</summary>
    public ImmutableArray<AbilitySyntax> Abilities { get; init; } = [];

    /// <summary>Gets the inbound triggers the extension raises.</summary>
    public ImmutableArray<ExtensionTrigger> Triggers { get; init; } = [];

    /// <summary>Gets the frontend components the extension provides.</summary>
    public ImmutableArray<ExtensionComponent> Components { get; init; } = [];

    /// <summary>Gets the backend tier, or <see langword="null"/> for a frontend-only package.</summary>
    public ExtensionBackend? Backend { get; init; }

    /// <summary>Gets the package-relative path of the frontend entry point, or <see langword="null"/> without a frontend.</summary>
    public string? Frontend { get; init; }
}

/// <summary>Represents an <c>image</c> line of the manifest.</summary>
/// <param name="Path">The package-relative path.</param>
/// <param name="Caption">The optional caption.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the line.</param>
public record ExtensionImage(string Path, string? Caption, SourceLocation Location);

/// <summary>Represents a <c>requires</c> line: a compatible Stage or Screenplay range, a platform or a runtime.</summary>
/// <param name="Kind">One of <c>stage</c>, <c>screenplay</c>, <c>platform</c> or <c>runtime</c>.</param>
/// <param name="Value">The range, platform name or runtime description.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the line.</param>
public record ExtensionRequirement(string Kind, string Value, SourceLocation Location);

/// <summary>Represents a <c>setting</c> line of the contract.</summary>
/// <param name="Name">The setting name.</param>
/// <param name="Exposure">How far the value travels.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the line.</param>
public record ExtensionSetting(string Name, ExtensionSettingExposure Exposure, SourceLocation Location);

/// <summary>Represents a <c>trigger</c> the extension raises into the model.</summary>
/// <param name="Name">The trigger name.</param>
/// <param name="Payload">The typed payload properties.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
public record ExtensionTrigger(string Name, ImmutableArray<PropertySyntax> Payload, SourceLocation Location);

/// <summary>Represents a frontend <c>component</c> of the extension.</summary>
/// <param name="Name">The component name.</param>
/// <param name="Kind">One of <c>react</c>, <c>mount</c> or <c>screenplay</c>.</param>
/// <param name="Platforms">The supported platforms.</param>
/// <param name="Placeholder">The package-relative path of the static design-time placeholder.</param>
/// <param name="Properties">The typed properties.</param>
/// <param name="Signals">The signals the component raises.</param>
/// <param name="Invokes">The abilities the component may call through the host.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
public record ExtensionComponent(
    string Name,
    string? Kind,
    ImmutableArray<string> Platforms,
    string? Placeholder,
    ImmutableArray<PropertySyntax> Properties,
    ImmutableArray<ExtensionSignal> Signals,
    ImmutableArray<string> Invokes,
    SourceLocation Location);

/// <summary>Represents a signal a component <c>raises</c>.</summary>
/// <param name="Name">The signal name.</param>
/// <param name="Values">The typed values it carries.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
public record ExtensionSignal(string Name, ImmutableArray<PropertySyntax> Values, SourceLocation Location);

/// <summary>Represents the <c>backend</c> line.</summary>
/// <param name="Runtime">The runtime name, for example <c>dotnet</c>.</param>
/// <param name="Version">The runtime version.</param>
/// <param name="Assembly">The package-relative entry point.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the line.</param>
public record ExtensionBackend(string Runtime, string Version, string Assembly, SourceLocation Location);

/// <summary>Holds the vocabulary of the contract file.</summary>
public static class ExtensionVocabulary
{
    /// <summary>The component kinds.</summary>
    public static readonly ImmutableArray<string> ComponentKinds = ["react", "mount", "screenplay"];

    /// <summary>The platforms a component or package can support.</summary>
    public static readonly ImmutableArray<string> Platforms = ["web", "ios", "android", "desktop"];

    /// <summary>The requirement kinds of a <c>requires</c> line.</summary>
    public static readonly ImmutableArray<string> RequirementKinds = ["stage", "screenplay", "platform", "runtime"];
}
