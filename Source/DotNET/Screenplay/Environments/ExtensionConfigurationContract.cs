// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Environments;

/// <summary>
/// Represents what an extension's contract says about its settings: the part of the contract that configuring a
/// system needs.
/// </summary>
/// <remarks>
/// The model does not carry a contract; the extension package does. A tool that has restored the package passes this
/// to <see cref="SystemConfigurationContracts.Check"/> and <see cref="EnvironmentRequirements.For(Syntax.ApplicationSyntax, IEnumerable{ExtensionConfigurationContract})"/>.
/// </remarks>
/// <param name="Reference">The reference the model names on its <c>extension</c> line, exactly as written.</param>
/// <param name="Settings">The settings the contract declares.</param>
public sealed record ExtensionConfigurationContract(string Reference, ImmutableArray<ExtensionSettingContract> Settings);

/// <summary>
/// Represents one setting an extension's contract declares.
/// </summary>
/// <param name="Name">The setting name.</param>
/// <param name="IsSecret">Whether the setting is secret; otherwise it is public and may reach the browser.</param>
/// <param name="Description">The optional description the contract gives.</param>
public sealed record ExtensionSettingContract(string Name, bool IsSecret, string? Description = null);
