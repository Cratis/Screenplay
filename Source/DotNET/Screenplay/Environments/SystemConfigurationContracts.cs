// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Environments;

/// <summary>
/// Checks the settings a model maps for its systems against what each extension's contract declares.
/// </summary>
public static class SystemConfigurationContracts
{
    /// <summary>
    /// Checks every system that names an extension whose contract is supplied.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> of a successful compilation.</param>
    /// <param name="contracts">The <see cref="ExtensionConfigurationContract"/> of each extension the model names; a system whose extension has none is not checked.</param>
    /// <returns>The diagnostics: an unknown setting, a literal on a secret setting and a secret mapped to a public setting.</returns>
    public static ImmutableArray<Diagnostic> Check(ApplicationSyntax application, IEnumerable<ExtensionConfigurationContract> contracts)
    {
        var byReference = contracts.ToDictionary(contract => contract.Reference, StringComparer.Ordinal);
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var system in application.Systems)
        {
            if (system.Extension is null || !byReference.TryGetValue(system.Extension.Reference, out var contract)) continue;
            var declared = contract.Settings.ToDictionary(setting => setting.Name, StringComparer.Ordinal);
            foreach (var setting in system.Settings)
            {
                if (!declared.TryGetValue(setting.Name, out var found))
                {
                    diagnostics.Add(Diagnostic.Error(DiagnosticCodes.UnknownSystemSetting, $"System '{system.Name}' maps setting '{setting.Name}', which extension '{contract.Reference}' does not declare.", setting.Location));
                }
                else if (setting.Secret is null && found.IsSecret)
                {
                    diagnostics.Add(Diagnostic.Error(DiagnosticCodes.SecretSettingLiteral, $"System '{system.Name}' gives setting '{setting.Name}' a literal value, but extension '{contract.Reference}' declares it secret; map it from a secret.", setting.Location));
                }
                else if (setting.Secret is not null && !found.IsSecret)
                {
                    diagnostics.Add(Diagnostic.Error(DiagnosticCodes.SecretOnPublicSetting, $"System '{system.Name}' maps secret '{setting.Secret}' to setting '{setting.Name}', which extension '{contract.Reference}' declares public and may reach the browser.", setting.Location));
                }
            }
        }

        return diagnostics.ToImmutable();
    }
}
