// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.ContractCatalog;

internal static class DiagnosticArgumentContracts
{
    internal static string Initializer(IFieldSymbol field)
    {
        var attribute = field.GetAttributes().SingleOrDefault(attribute => attribute.AttributeClass?.Name == "DiagnosticArgumentDefinitionsAttribute");
        if (attribute is null) return string.Empty;
        var output = new StringBuilder(" { Arguments = new DiagnosticArgumentFact[] {");
        foreach (var definition in attribute.ConstructorArguments[0].Values)
        {
            var declaration = ((string)definition.Value!).Split(';');
            var parts = declaration[0].Split(':');
            var type = parts[1].Split('=');
            var optional = type[0].EndsWith('?');
            output.Append("new(").Append(Quote(parts[0])).Append(',').Append(Quote(type[0].TrimEnd('?'))).Append(',').Append(optional ? "true" : "false")
                .Append(',').Append(Quote(declaration[1])).Append(", new string[] {").AppendJoin(',', (type.Length > 1 ? type[1].Split('|') : []).Select(Quote)).Append("}),");
        }
        return output.Append("} }").ToString();
    }

    internal static void Check(GeneratorExecutionContext context, SyntaxTree[] trees)
    {
        foreach (var tree in trees)
        {
            var model = context.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method || method.Name is not ("Error" or "Warning" or "Information")) continue;
                var arguments = invocation.ArgumentList.Arguments;
                if (arguments.Count == 0 || model.GetSymbolInfo(arguments[0].Expression).Symbol is not IFieldSymbol field || field.ContainingType.ToDisplayString() != "Cratis.Screenplay.Diagnostics.DiagnosticCodes") continue;
                if (!field.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "DiagnosticArgumentDefinitionsAttribute")) continue;
                if (arguments.Count >= 4) continue;
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SPCON003", "Diagnostic arguments missing", "Diagnostic {0} declares arguments; pass its subjects to the reporting sink", "Contract", DiagnosticSeverity.Error, true), invocation.GetLocation(), field.Name));
            }
        }
    }

    static string Quote(string value) => SymbolDisplay.FormatLiteral(value, true);
}
