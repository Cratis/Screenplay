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

    /// <summary>
    /// Gets requirements that need nothing from the environment.
    /// </summary>
    public static EnvironmentRequirements None { get; } = new(ImmutableArray<EnvironmentKey>.Empty);

    /// <summary>
    /// Gets the keys that must be supplied as secrets.
    /// </summary>
    public IEnumerable<EnvironmentKey> Secrets => Keys.Where(key => key.IsSecret);

    /// <summary>
    /// Derives the environment values a model needs from its syntax.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> of a successful compilation.</param>
    /// <returns>The <see cref="EnvironmentRequirements"/> of the model.</returns>
    public static EnvironmentRequirements For(ApplicationSyntax application)
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

        return keys.Count == 0 ? None : new(keys.ToImmutable());
    }

    /// <summary>
    /// Gets the key of one setting of a provider.
    /// </summary>
    /// <param name="provider">The <see cref="AuthenticationProviderSyntax"/>.</param>
    /// <param name="setting">The name of the setting, for example <c>clientId</c>.</param>
    /// <returns>The key to ask the environment for.</returns>
    public static string ProviderKey(AuthenticationProviderSyntax provider, string setting) => $"{ProviderPrefix}{provider.Identity}.{setting}";

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
