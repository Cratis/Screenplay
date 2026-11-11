// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates secrets and system settings against the application's declarations: what the model alone can decide. What
/// an extension's contract declares is checked by <see cref="Environments.SystemConfigurationContracts"/>.
/// </summary>
internal static class SystemConfigurationValidator
{
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var declared = new Dictionary<string, SecretSyntax>(StringComparer.OrdinalIgnoreCase);
        foreach (var secret in application.Secrets)
        {
            if (declared.TryAdd(secret.Name, secret)) continue;
            context.Error(DiagnosticCodes.DuplicateSecret, $"Secret '{secret.Name}' is declared more than once; secret names compare ignoring case.", secret.Location);
        }

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var system in application.Systems)
        {
            var mapped = new HashSet<string>(StringComparer.Ordinal);
            foreach (var setting in system.Settings)
            {
                if (!mapped.Add(setting.Name))
                {
                    context.Error(DiagnosticCodes.DuplicateSystemSetting, $"System '{system.Name}' maps setting '{setting.Name}' more than once.", setting.Location);
                    continue;
                }

                if (setting.Secret is null) continue;
                if (declared.ContainsKey(setting.Secret)) used.Add(setting.Secret);
                else context.Error(DiagnosticCodes.UnknownSecret, $"System '{system.Name}' setting '{setting.Name}' maps from secret '{setting.Secret}', which is not declared.", setting.Location);
            }
        }

        foreach (var secret in declared.Values.Where(secret => !used.Contains(secret.Name)))
            context.Error(DiagnosticCodes.UnusedSecret, $"Secret '{secret.Name}' is declared but no system setting maps from it.", secret.Location);
    }
}
