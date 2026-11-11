// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Environments;

/// <summary>
/// Represents the environment values a model needs to run, named by what the model declares.
/// </summary>
/// <remarks>
/// A model says what an application is; an environment says where one instance of it runs. The two meet by
/// name: every key here is derived from a declaration, so a runtime can ask an environment for exactly these
/// and report what is missing before it starts instead of failing at the first sign-in or the first mail.
/// <para>
/// The convention is stated in <c>Documentation/screenplay/environments.md</c>. In short:
/// </para>
/// <list type="bullet">
/// <item><description><c>authentication.providers.&lt;Identity&gt;.clientId</c> and <c>.clientSecret</c> for every provider, where <c>Identity</c> is its alias when it has one and its name otherwise; <c>.tenantId</c> for <c>EntraId</c> and <c>.authority</c> for <c>OpenId</c>.</description></item>
/// <item><description><c>onboarding.email.sender</c> and <c>onboarding.email.server.host</c>, <c>.port</c>, <c>.username</c> and <c>.password</c> when the model declares onboarding.</description></item>
/// <item><description><c>secrets.&lt;Name&gt;</c> for every declared <c>secret</c>, once, however many systems map from it.</description></item>
/// <item><description><c>systems.&lt;System&gt;.&lt;setting&gt;</c> for every setting an extension's contract declares and the model does not map, when the contracts are supplied.</description></item>
/// </list>
/// A key marked <see cref="EnvironmentKey.IsSecret"/> is referenced, never carried: only the key travels.
/// </remarks>
/// <param name="Keys">Every <see cref="EnvironmentKey"/> the model needs, in declaration order.</param>
public sealed record EnvironmentRequirements(ImmutableArray<EnvironmentKey> Keys)
{
    /// <summary>The prefix of every provider key.</summary>
    public const string ProviderPrefix = "authentication.providers.";

    /// <summary>The prefix of every onboarding key.</summary>
    public const string OnboardingPrefix = "onboarding.";

    /// <summary>The prefix of every named secret key.</summary>
    public const string SecretPrefix = "secrets.";

    /// <summary>The prefix of every implicit system setting key.</summary>
    public const string SystemPrefix = "systems.";

    /// <summary>
    /// Gets requirements that need nothing from the environment.
    /// </summary>
    public static EnvironmentRequirements None { get; } = new([]);

    /// <summary>
    /// Gets the keys that must be supplied as secrets.
    /// </summary>
    public IEnumerable<EnvironmentKey> Secrets => Keys.Where(key => key.IsSecret);

    /// <summary>
    /// Derives the environment values a model needs from its syntax.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> of a successful compilation.</param>
    /// <returns>The <see cref="EnvironmentRequirements"/> of the model.</returns>
    public static EnvironmentRequirements For(ApplicationSyntax application) => For(application, []);

    /// <summary>
    /// Derives the environment values a model needs from its syntax and the contracts of the extensions its systems name.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> of a successful compilation.</param>
    /// <param name="contracts">The <see cref="ExtensionConfigurationContract"/> of each extension the model names; without one, a system's implicit settings cannot be listed.</param>
    /// <returns>The <see cref="EnvironmentRequirements"/> of the model.</returns>
    public static EnvironmentRequirements For(ApplicationSyntax application, IEnumerable<ExtensionConfigurationContract> contracts)
    {
        var keys = ImmutableArray.CreateBuilder<EnvironmentKey>();
        foreach (var provider in application.Authentication?.Providers ?? [])
        {
            AddProvider(keys, provider);
        }

        if (application.Onboarding is { } onboarding)
        {
            AddOnboarding(keys, onboarding);
        }

        foreach (var secret in application.Secrets)
        {
            keys.Add(new(SecretKey(secret.Name), EnvironmentKeySource.Secret, secret.Name, secret.Description ?? $"The secret '{secret.Name}'.", true, secret.Location));
        }

        AddSystemSettings(keys, application, contracts);

        return keys.Count == 0 ? None : new(keys.ToImmutable());
    }

    /// <summary>
    /// Gets the key of one setting of a provider.
    /// </summary>
    /// <param name="provider">The <see cref="AuthenticationProviderSyntax"/>.</param>
    /// <param name="setting">The name of the setting, for example <c>clientId</c>.</param>
    /// <returns>The key to ask the environment for.</returns>
    public static string ProviderKey(AuthenticationProviderSyntax provider, string setting) => $"{ProviderPrefix}{provider.Identity}.{setting}";

    /// <summary>
    /// Gets the key of a named secret.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>The key to ask the environment for.</returns>
    public static string SecretKey(string name) => $"{SecretPrefix}{name}";

    /// <summary>
    /// Gets the key of one setting of a system.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="setting">The name of the setting.</param>
    /// <returns>The key to ask the environment for.</returns>
    public static string SystemKey(string system, string setting) => $"{SystemPrefix}{system}.{setting}";

    static void AddSystemSettings(ImmutableArray<EnvironmentKey>.Builder keys, ApplicationSyntax application, IEnumerable<ExtensionConfigurationContract> contracts)
    {
        var byReference = contracts.ToDictionary(contract => contract.Reference, StringComparer.Ordinal);
        foreach (var system in application.Systems)
        {
            if (system.Extension is null || !byReference.TryGetValue(system.Extension.Reference, out var contract)) continue;
            var mapped = system.Settings.Select(setting => setting.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var setting in contract.Settings.Where(setting => !mapped.Contains(setting.Name)))
            {
                keys.Add(new(SystemKey(system.Name, setting.Name), EnvironmentKeySource.SystemSetting, system.Name, setting.Description ?? $"The {setting.Name} setting of the {system.Name} system.", setting.IsSecret, system.Location));
            }
        }
    }

    static void AddProvider(ImmutableArray<EnvironmentKey>.Builder keys, AuthenticationProviderSyntax provider)
    {
        void Add(string setting, string description, bool isSecret = false) =>
            keys.Add(new(ProviderKey(provider, setting), EnvironmentKeySource.AuthenticationProvider, provider.Identity, description, isSecret, provider.Location));

        Add("clientId", $"The client id the application is registered with at {provider.Name}.");
        Add("clientSecret", $"The client secret that goes with the client id at {provider.Name}.", isSecret: true);
        switch (provider.Name)
        {
            case "EntraId":
                Add("tenantId", "The Microsoft Entra directory (tenant) that signs people in.");
                break;
            case "OpenId":
                Add("authority", "The base address of the OpenID Connect authority that signs people in.");
                break;
        }
    }

    static void AddOnboarding(ImmutableArray<EnvironmentKey>.Builder keys, OnboardingSyntax onboarding)
    {
        void Add(string setting, string description, bool isSecret = false) =>
            keys.Add(new($"{OnboardingPrefix}email.{setting}", EnvironmentKeySource.Onboarding, "onboarding", description, isSecret, onboarding.Location));

        Add("sender", "The address onboarding emails are sent from.");
        Add("server.host", "The mail server onboarding emails are sent through.");
        Add("server.port", "The port of the mail server.");
        Add("server.username", "The user name to sign in to the mail server with.");
        Add("server.password", "The password to sign in to the mail server with.", isSecret: true);
    }
}
