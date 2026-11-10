// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison;

sealed record AgentExposureSnapshot(bool HasErrors, bool HasBlock, AgentTokenKind? Tokens, IReadOnlyDictionary<AgentExposureSnapshot.Key, AgentExposureSnapshot.Operation> Operations)
{
    internal static AgentExposureSnapshot Read(AuthoringSnapshot source)
    {
        // This is parsing/validation only, never the workspace's executable-binding result.
        var compilation = source.Compilation;
        var applications = compilation.Value is { } application ? [application] : source.Sources
            .Select(pair => new ScreenplayCompiler(source.Languages).Parse(pair.Value, pair.Key).Value).OfType<ApplicationSyntax>().ToArray();
        var blocks = applications.Select(value => value.Agents).OfType<AgentsSyntax>().ToArray();
        var targets = applications.SelectMany(AgentOperationCatalog.Of).ToArray();
        var operations = blocks.SelectMany(block => block.Operations).Select(operation => Resolve(operation, targets, source.Index, compilation.Success))
            .GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.First().Value);
        return new(!compilation.Success, blocks.Length > 0, blocks.FirstOrDefault()?.Tokens?.Kind, operations);
    }

    static KeyValuePair<Key, Operation> Resolve(AgentOperationSyntax operation, IReadOnlyList<AgentOperationCatalog.Target> targets, AuthoringIndex index, bool sourceValid)
    {
        var candidates = targets.Where(target => target.Kind == operation.Kind).ToArray();
        var resolution = ReferenceResolver.Resolve(operation.Reference, new([]), [.. candidates.Select(target => target.Declaration)]);
        var name = resolution.Resolved is { } resolved ? candidates.Single(target => target.Declaration == resolved).QualifiedName : operation.Reference;
        var declarations = index.Declarations.Where(declaration => declaration.Kind == operation.Kind.ToString() && declaration.Address == name).ToArray();
        var signature = sourceValid && declarations.Length == 1 ? AgentExposureSignature.Read(index, declarations[0]) : null;
        return new(new(operation.Kind, name), new(operation.Scope, signature));
    }

    internal sealed record Key(AgentOperationKind Kind, string Name);

    internal sealed record Operation(AgentScope? Scope, string? Signature);
}
