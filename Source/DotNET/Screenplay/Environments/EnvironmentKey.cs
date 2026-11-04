// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Environments;

/// <summary>
/// Represents one value the environment an application runs in has to supply, found by name.
/// </summary>
/// <param name="Key">The dotted key the environment is asked for, for example <c>authentication.providers.EntraId.clientId</c>.</param>
/// <param name="Source">The kind of declaration in the model that needs the value.</param>
/// <param name="Owner">The declaration that needs it - the <c>Identity</c> of a provider, or <c>onboarding</c>.</param>
/// <param name="Description">What the value is, in a sentence a person setting up an environment can act on.</param>
/// <param name="IsSecret">Whether the value is a secret. A secret is only ever referenced by its key and never written into the model, the environment's plain values or a log.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration that needs the value.</param>
public record EnvironmentKey(
    string Key,
    EnvironmentKeySource Source,
    string Owner,
    string Description,
    bool IsSecret,
    SourceLocation Location);
