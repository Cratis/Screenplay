// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Checks the files a contract points at: the icon, the images, the frontend and the placeholders.
/// </summary>
public static class ExtensionPackageCheck
{
    /// <summary>
    /// Checks a package directory against its contract.
    /// </summary>
    /// <param name="contract">The parsed contract.</param>
    /// <param name="packageRoot">The directory that holds <c>contract.play</c> and the files it names.</param>
    /// <returns>The diagnostics.</returns>
    public static IReadOnlyList<Diagnostic> Check(ExtensionContract contract, string packageRoot)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var (kind, path, location) in Files(contract))
        {
            var full = Path.GetFullPath(Path.Combine(packageRoot, path));
            if (!full.StartsWith(Path.GetFullPath(packageRoot), StringComparison.Ordinal))
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionContract, $"The {kind} '{path}' is outside the package.", location));
            }
            else if (!File.Exists(full))
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionContract, $"The {kind} '{path}' is not in the package.", location));
            }
            else if (kind == "placeholder")
            {
                foreach (var violation in ExtensionPlaceholder.Violations(Encoding.UTF8.GetString(File.ReadAllBytes(full))))
                {
                    diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionPlaceholder, $"Placeholder '{path}' {violation}.", location));
                }
            }
        }

        return diagnostics;
    }

    static IEnumerable<(string Kind, string Path, SourceLocation Location)> Files(ExtensionContract contract)
    {
        if (contract.Icon is not null) yield return ("icon", contract.Icon, contract.Location);
        foreach (var image in contract.Images) yield return ("image", image.Path, image.Location);
        if (contract.Frontend is not null) yield return ("frontend", contract.Frontend, contract.Location);
        foreach (var component in contract.Components.Where(component => component.Placeholder is not null)) yield return ("placeholder", component.Placeholder!, component.Location);
    }
}
