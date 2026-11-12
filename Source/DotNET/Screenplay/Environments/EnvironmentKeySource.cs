// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Environments;

/// <summary>
/// The kind of model declaration an <see cref="EnvironmentKey"/> belongs to.
/// </summary>
public enum EnvironmentKeySource
{
    /// <summary>A <c>provider</c> of the <c>authentication</c> block.</summary>
    AuthenticationProvider,

    /// <summary>The <c>onboarding</c> block, which sends mail.</summary>
    Onboarding,

    /// <summary>A <c>secret</c> declaration, which a deployment binds to its store.</summary>
    Secret,

    /// <summary>A setting of a <c>system</c> the model does not map, read from configuration under <c>systems.&lt;System&gt;.&lt;setting&gt;</c>.</summary>
    SystemSetting
}
