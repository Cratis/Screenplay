// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static class OperationValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var resolver = declarations.Productions;
        var values = new ResponseValueTypes(application);
        var systems = application.Systems.ToLookup(system => system.Name, StringComparer.Ordinal);
        foreach (var duplicate in systems.SelectMany(group => group.Skip(1)))
            context.Error(DiagnosticCodes.InvalidSystemDeclaration, $"System '{duplicate.Name}' is declared more than once.", duplicate.Location);
        var knownTypes = ConceptSyntax.PrimitiveTypes.Concat(application.Concepts.Select(node => node.Name)).Concat((application.Types ?? []).Select(node => node.Name))
            .Concat(application.Imports.Select(node => node.Name)).ToHashSet(StringComparer.Ordinal);
        foreach (var (slice, scope) in declarations.Slices)
        {
            var operations = OperationDeclarations.In(slice).ToArray();
            var eventNames = EventDeclarations.In(slice).Select(node => node.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var group in operations.GroupBy(node => node.Name, StringComparer.Ordinal))
            {
                foreach (var node in eventNames.Contains(group.Key) ? group : group.Skip(1))
                    context.Error(DiagnosticCodes.ProductionDeclarationCollision, $"Operation '{node.Name}' collides with another event or operation in slice '{slice.Name}'.", node.Location);
            }
            foreach (var operation in operations)
            {
                if (operation.Uses.Length > 0 && systems[operation.Uses].Count() != 1)
                    context.Error(DiagnosticCodes.InvalidSystemReference, $"Operation '{operation.Name}' must use one declared system; '{operation.Uses}' does not resolve uniquely.", operation.DirectiveLocations.GetValueOrDefault("uses", operation.Location));
                foreach (var input in operation.Inputs)
                {
                    if (!knownTypes.Contains(input.Type.Name)) context.Warning(DiagnosticCodes.UnknownType, $"Unknown type '{input.Type.Name}' on input '{input.Name}' of operation '{operation.Name}'.", input.Type.Location);
                }
            }
            foreach (var command in slice.Commands)
            {
                foreach (var production in command.Produces)
                {
                    var resolution = resolver.Resolve(production.Event, slice);
                    if (resolution.Kind == AuthoringProductionKind.Ambiguous && resolution.Candidates.Any(node => node.Kind == AuthoringProductionKind.Operation))
                        context.Error(DiagnosticCodes.InvalidProductionReference, $"Production '{production.Event}' is ambiguous.", production.Location);
                    if (production.Event.Contains('.', StringComparison.Ordinal) && resolution.Kind != AuthoringProductionKind.Operation)
                        context.Error(DiagnosticCodes.InvalidProductionReference, "Qualified productions are supported only for explicitly declared operations.", production.Location);
                    if (resolution.Declaration?.Node is not OperationSyntax operation) continue;
                    if (production.For is not null || (production.Tags ?? []).Any()) context.Error(DiagnosticCodes.InvalidOperationDeclaration, "Operation productions cannot declare event destinations or tags.", production.Location);
                    ValidateMappings(production.Mappings, operation, command, scope, declarations, values, context, required: true);
                }
            }
            foreach (var production in slice.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(ReactionProductions.In))
            {
                var resolution = resolver.Resolve(production.Event, slice);
                if (resolution.Kind == AuthoringProductionKind.Ambiguous && resolution.Candidates.Any(node => node.Kind == AuthoringProductionKind.Operation))
                    context.Error(DiagnosticCodes.InvalidProductionReference, $"Production '{production.Event}' is ambiguous.", production.Location);
                if (production.Event.Contains('.', StringComparison.Ordinal) && resolution.Kind != AuthoringProductionKind.Operation)
                    context.Error(DiagnosticCodes.InvalidProductionReference, "Qualified productions are supported only for explicitly declared operations.", production.Location);
                if (resolver.IsOperation(production, slice)) context.Error(DiagnosticCodes.OperationOutsideCommand, "Operations can only be produced by commands.", production.Location);
            }
            foreach (var specification in slice.Specifications)
            {
                var steps = specification.GivenOperationFailures.Select(node => (node.Operation, Node: (SyntaxNode)node))
                    .Concat(specification.ThenOperations.Select(node => (node.Operation, Node: (SyntaxNode)node)))
                    .Concat(specification.ThenCompensated.Select(node => (node.Operation, Node: (SyntaxNode)node))).ToArray();
                if (steps.Length == 0) continue;
                var command = specification.When is { } when ? declarations.Resolve(when.CommandType, scope, node => node.Commands, node => node.Name)?.Node : null;
                if (command is null || specification.WhenAppended is not null || specification.WhenQuery is not null || specification.WhenTrigger is not null || specification.WhenClock is not null || specification.WhenCapture is not null)
                    context.Error(DiagnosticCodes.InvalidOperationSpecification, "Operation fixtures and assertions require a declared command action.", specification.Location);
                var duplicates = new HashSet<(Type Kind, SyntaxNode Operation)>();
                foreach (var step in steps)
                {
                    if (step.Node is SpecificationOperationSyntax concreteAssertion)
                    {
                        foreach (var mapping in concreteAssertion.Values.Where(mapping => !Concrete(mapping.Source)))
                        {
                            context.Error(DiagnosticCodes.InvalidOperationSpecification, "Operation assertions require concrete input values.", mapping.Source.Location);
                        }
                    }
                    var resolution = resolver.Resolve(step.Operation, slice);
                    if (resolution.Declaration?.Node is not OperationSyntax operation)
                    {
                        context.Error(DiagnosticCodes.InvalidOperationSpecification, $"'{step.Operation}' does not resolve uniquely to a declared operation.", step.Node.Location);
                        continue;
                    }
                    if (!duplicates.Add((step.Node.GetType(), operation))) context.Error(DiagnosticCodes.InvalidOperationSpecification, $"Duplicate operation step '{step.Operation}'.", step.Node.Location);
                    if (step.Node is SpecificationCompensatedSyntax && operation.Compensate is null)
                        context.Error(DiagnosticCodes.InvalidOperationSpecification, $"Operation '{step.Operation}' declares no compensation.", step.Node.Location);
                    if (command is not null && command.Handler is null)
                    {
                        var commandSlice = declarations.Slices.First(entry => entry.Slice.Commands.Any(node => ReferenceEquals(node, command))).Slice;
                        var produced = command.Produces.Select(production => resolver.Resolve(production.Event, commandSlice)).ToArray();
                        if (produced.All(result => result.Kind is AuthoringProductionKind.Operation or AuthoringProductionKind.Event) && !produced.Any(result => ReferenceEquals(result.Declaration?.Node, operation)))
                            context.Error(DiagnosticCodes.InvalidOperationSpecification, $"Command '{command.Name}' does not produce operation '{step.Operation}'.", step.Node.Location);
                    }
                    if (step.Node is SpecificationOperationSyntax assertion) ValidateMappings(assertion.Values, operation, null, scope, declarations, values, context, required: false);
                }
            }
        }
    }

    static void ValidateMappings(IEnumerable<PropertyMappingSyntax> source, OperationSyntax operation, CommandSyntax? command, DeclarationScope scope, ConsistencyDeclarations declarations, ResponseValueTypes values, ParserContext context, bool required)
    {
        var mappings = source.ToArray();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var allNames = mappings.Select(mapping => mapping.Property).ToHashSet(StringComparer.Ordinal);
        var suppliedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            var segments = mapping.Property.Split('.');
            for (var depth = 1; depth <= segments.Length; depth++) suppliedPaths.Add(string.Join('.', segments.Take(depth)));
        }
        foreach (var mapping in mappings)
        {
            if (!names.Add(mapping.Property)) context.Error(DiagnosticCodes.InvalidOperationMapping, $"Duplicate operation input mapping '{mapping.Property}'.", mapping.Location);
            var target = PathType(operation.Inputs, mapping.Property, declarations, out var missing, inheritOptionality: false);
            if (missing) context.Error(DiagnosticCodes.InvalidOperationMapping, $"Operation '{operation.Name}' declares no input '{mapping.Property}'.", mapping.Location);
            if (target is null) continue;
            var targetSegments = mapping.Property.Split('.');
            for (var depth = 1; depth < targetSegments.Length; depth++)
            {
                if (allNames.Contains(string.Join('.', targetSegments.Take(depth))))
                {
                    context.Error(DiagnosticCodes.InvalidOperationMapping, $"Operation input mapping '{mapping.Property}' overlaps a whole input mapping.", mapping.Location);
                }
            }
            if (command is not null && mapping.Source is PathExpressionSyntax path)
            {
                var supplied = PathType(command.Properties, path.Path, declarations, out var sourceMissing);
                var segments = path.Path.Split('.');
                var reads = (command.Reads ?? []).Where(read => (read.Alias ?? read.ReadModel) == segments[0]).ToArray();
                if (sourceMissing && reads.Length > 0)
                {
                    sourceMissing = false;
                    if (reads.Length == 1 && segments.Length > 1 && declarations.ViewProperties(reads[0].ReadModel, scope) is { } properties)
                        supplied = PathType(properties, string.Join('.', segments.Skip(1)), declarations, out sourceMissing);
                }
                if (sourceMissing) context.Error(DiagnosticCodes.InvalidOperationMapping, $"Command '{command.Name}' declares no source '{path.Path}'.", mapping.Source.Location);
                if (supplied is not null && declarations.Compatible(supplied, target) == false)
                    context.Error(DiagnosticCodes.InvalidOperationMapping, $"Source '{path.Path}' is incompatible with input '{mapping.Property}' of operation '{operation.Name}'.", mapping.Source.Location);
            }
            else if (mapping.Source is LiteralExpressionSyntax or ObjectExpressionSyntax or ListExpressionSyntax)
            {
                if (!values.Compatible(mapping.Source, target) || (required && !CompleteValue(mapping.Source, target, declarations))) context.Error(DiagnosticCodes.InvalidOperationMapping, $"Value is incompatible with input '{mapping.Property}' of operation '{operation.Name}'.", mapping.Source.Location);
            }
        }
        if (!required) return;
        foreach (var input in operation.Inputs)
        {
            if (!Covered(input.Type, input.Name, names, suppliedPaths, declarations, new HashSet<string>(StringComparer.Ordinal)))
                context.Error(DiagnosticCodes.InvalidOperationMapping, $"Production of operation '{operation.Name}' supplies no value for required input '{input.Name}'.", operation.Location);
        }
    }

    static bool CompleteValue(ExpressionSyntax value, TypeRefSyntax type, ConsistencyDeclarations declarations)
    {
        if (value is ListExpressionSyntax list && type.IsCollection)
            return list.Items.All(item => CompleteValue(item, type with { IsCollection = false, IsOptional = false }, declarations));
        if (value is not ObjectExpressionSyntax obj || declarations.TypeProperties(type.Name) is not { } properties) return true;
        var members = obj.Members.ToLookup(member => member.Name, StringComparer.Ordinal);

        return properties.All(property =>
            (property.Type.IsOptional || members[property.Name].Any()) &&
            members[property.Name].All(member => CompleteValue(member.Value, property.Type, declarations)));
    }

    static bool Concrete(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax { Value: double number } => double.IsFinite(number),
        LiteralExpressionSyntax => true,
        ObjectExpressionSyntax obj => obj.Members.All(member => Concrete(member.Value)),
        ListExpressionSyntax list => list.Items.All(Concrete),
        _ => false
    };

    static bool Covered(TypeRefSyntax type, string path, HashSet<string> mappings, HashSet<string> suppliedPaths, ConsistencyDeclarations declarations, HashSet<string> seen)
    {
        if (mappings.Contains(path) || (type.IsOptional && !suppliedPaths.Contains(path))) return true;
        if (type.IsCollection || !seen.Add(type.Name)) return false;
        if (declarations.TypeProperties(type.Name) is not { } properties)
            return suppliedPaths.Contains(path) && declarations.Compatible(type, type) is null;

        return properties.All(property => Covered(property.Type, $"{path}.{property.Name}", mappings, suppliedPaths, declarations, new(seen, StringComparer.Ordinal)));
    }

    static TypeRefSyntax? PathType(IEnumerable<PropertySyntax> properties, string path, ConsistencyDeclarations declarations, out bool missing, bool inheritOptionality = true)
    {
        var property = declarations.Property(properties, path, out missing);
        if (property is null) return null;
        var type = property.Type;
        var segments = path.Split('.');
        for (var index = 0; index < segments.Length - 1; index++)
        {
            var parent = declarations.Property(properties, string.Join('.', segments.Take(index + 1)), out _);
            if (parent is not null) type = type with { IsOptional = type.IsOptional || (inheritOptionality && parent.Type.IsOptional), IsCollection = type.IsCollection || parent.Type.IsCollection };
        }

        return type;
    }
}
