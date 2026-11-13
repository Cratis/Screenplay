// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        ImmutableArray<SemanticProvidedValue> BindProvides(IEnumerable<ProvideSyntax> provides)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticProvidedValue>();
            foreach (var provide in provides)
            {
                if (BindProvide(provide) is { } value) bound.Add(value);
            }

            return bound.ToImmutable();
        }

        SemanticProvidedValue? BindProvide(ProvideSyntax provide)
        {
            var source = BindProvideSource(provide);
            if (source is null) return null;
            if (!_readModels.TryGetValue(provide.Type.Name, out var readModel))
            {
                return new(provide.Name, source) { Type = BindTypeReference(provide.Type) };
            }

            return new(provide.Name, source) { ReadModel = readModel.Model.Id, Optional = provide.Type.IsOptional, Collection = provide.Type.IsCollection };
        }

        SemanticProvideSource? BindProvideSource(ProvideSyntax provide)
        {
            switch (provide.Source)
            {
                case HostProvideSourceSyntax:
                    return new SemanticHostProvideSource();
                case ValueProvideSourceSyntax value:
                    if (SemanticAvailabilityBinder.Operand(value.Value) is { } operand) return new SemanticValueProvideSource(operand);
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Provided value '{provide.Name}' must be a literal or a $context value to be carried by an executable model.", provide.Location);
                    return null;
                case QueryProvideSourceSyntax query:
                    var declared = _queryDeclarations.FirstOrDefault(entry => entry.Key.Name == query.Query).Value;
                    if (declared is null)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Provided value '{provide.Name}' reads query '{query.Query}', which is not an executable keyed query.", provide.Location);
                        return null;
                    }

                    var by = ImmutableArray.CreateBuilder<SemanticContextOperand>();
                    foreach (var expression in query.By)
                    {
                        if (SemanticAvailabilityBinder.Operand(expression) is not { } key)
                        {
                            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Provided value '{provide.Name}' keys query '{query.Query}' with something other than a literal or a $context value.", provide.Location);
                            return null;
                        }

                        by.Add(key);
                    }

                    return new SemanticQueryProvideSource(declared.Id, by.ToImmutable());
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Provided value '{provide.Name}' takes its value from a file or code block, which no supported executable model (ESM) version carries yet (#1300).", provide.Location);
                    return null;
            }
        }
    }
}
