// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.ContractCatalog;

internal static class DiagnosticArgumentContracts
{
    internal static string Initializer(GeneratorExecutionContext context, IFieldSymbol field)
    {
        var attribute = field.GetAttributes().SingleOrDefault(attribute => attribute.AttributeClass?.Name == "DiagnosticArgumentDefinitionsAttribute");
        if (attribute is null) return string.Empty;
        var output = new StringBuilder(" { Arguments = new DiagnosticArgumentFact[] {");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in attribute.ConstructorArguments[0].Values)
        {
            var declaration = definition.Value as string ?? string.Empty;
            var error = Parse(declaration, out var argument);
            if (error is null && !names.Add(argument.Name)) error = $"duplicate argument name '{argument.Name}'";
            if (error is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SPCON004", "Invalid diagnostic argument declaration", "Diagnostic {0} has invalid argument declaration '{1}': {2}", "Contract", DiagnosticSeverity.Error, true), field.Locations[0], field.Name, declaration, error));
                return string.Empty;
            }

            output.Append("new(").Append(Quote(argument.Name)).Append(',').Append(Quote(argument.Kind)).Append(',').Append(argument.Optional ? "true" : "false")
                .Append(',').Append(Quote(argument.Meaning)).Append(", new string[] {").AppendJoin(',', argument.Values.Select(Quote)).Append("}),");
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

    static string? Parse(string declaration, out Argument argument)
    {
        argument = new(string.Empty, string.Empty, false, string.Empty, []);
        var separator = declaration.IndexOf(';');
        if (separator < 0) return "expected name:kind[?][=values];meaning";
        var header = declaration[..separator];
        var meaning = declaration[(separator + 1)..];
        var colon = header.IndexOf(':');
        if (colon < 0) return "expected a name followed by ':' and a kind";
        var name = header[..colon];
        if (name.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(name[0]) || !name.All(SyntaxFacts.IsIdentifierPartCharacter)) return "argument name must be a nonempty identifier";
        var type = header[(colon + 1)..];
        var equals = type.IndexOf('=');
        var kind = equals < 0 ? type : type[..equals];
        var optional = kind.EndsWith('?');
        if (optional) kind = kind[..^1];
        if (kind is not ("text" or "keyword" or "index" or "declaration")) return $"unknown argument kind '{kind}'; expected text, keyword, index, or declaration";
        var values = equals < 0 ? [] : type[(equals + 1)..].Split('|');
        if (kind == "keyword" && (values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))) return "keyword arguments require a nonempty set of values";
        if (kind != "keyword" && equals >= 0) return "only keyword arguments may declare values";
        if (values.Any(value => value != value.Trim() || value.Any(character => character is '=' or ':' or '?'))) return "keyword values must not contain surrounding whitespace, '=', ':', or '?'";
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) return "keyword values must be unique";
        if (string.IsNullOrWhiteSpace(meaning)) return "argument meaning must not be empty";
        argument = new(name, kind, optional, meaning, values);

        return null;
    }

    static string Quote(string value) => SymbolDisplay.FormatLiteral(value, true);

    sealed record Argument(string Name, string Kind, bool Optional, string Meaning, string[] Values);
}
