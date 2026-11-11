// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents how a model configures one system: its extension, where each setting comes from, and its activation
/// override. Realization metadata carried by <see cref="SemanticCompilation.SystemConfigurations"/>, never part of the
/// executable semantic model.
/// </summary>
/// <param name="System">The system name, as declared.</param>
/// <param name="Extension">The extension package reference, or <see langword="null"/> when none is named.</param>
/// <param name="Version">The extension version, or <see langword="null"/> when none is written.</param>
/// <param name="Settings">The settings the model maps, in declaration order.</param>
/// <param name="Activation">The activation override, or <see langword="null"/> when the extension's default applies.</param>
public sealed record SemanticSystemConfiguration(
    string System,
    string? Extension,
    string? Version,
    ImmutableArray<SemanticSystemSetting> Settings,
    SemanticSystemActivation? Activation);

/// <summary>
/// Represents one mapped system setting: either read from a named secret or given a literal.
/// </summary>
/// <param name="Name">The setting name.</param>
/// <param name="Secret">The named secret the value comes from, or <see langword="null"/> for a literal.</param>
/// <param name="Value">The literal value, or <see langword="null"/> when it comes from a secret.</param>
public sealed record SemanticSystemSetting(string Name, string? Secret, string? Value);

/// <summary>
/// Represents an activation override on a system.
/// </summary>
/// <param name="Always">Whether the extension is always active; otherwise it is active on demand.</param>
/// <param name="IdleAmount">The idle amount before an on-demand extension stops, or <see langword="null"/>.</param>
/// <param name="IdleUnit">The idle unit, or <see langword="null"/>.</param>
public sealed record SemanticSystemActivation(bool Always, int? IdleAmount, string? IdleUnit);

/// <summary>
/// Represents a named secret the model declares, which systems map settings from. Realization metadata carried by
/// <see cref="SemanticCompilation.Secrets"/>.
/// </summary>
/// <param name="Name">The secret name.</param>
/// <param name="Description">The description, or <see langword="null"/>.</param>
public sealed record SemanticSecretDeclaration(string Name, string? Description);
