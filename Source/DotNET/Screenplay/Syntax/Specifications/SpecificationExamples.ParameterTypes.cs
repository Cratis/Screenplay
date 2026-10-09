// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Syntax.Specifications;

public static partial class SpecificationExamples
{
    private sealed partial class Expansion
    {
        void ValidateParameterReferences(SpecificationSyntax specification, DeclarationScope scope)
        {
            var values = new ResponseValueTypes(_application);
            foreach (var parameter in specification.Parameters)
            {
                if (!ConceptSyntax.PrimitiveTypes.Contains(parameter.Type.Name) && !_application.Concepts.Any(concept => concept.Name == parameter.Type.Name) && !(_application.Types ?? []).Any(type => type.Name == parameter.Type.Name))
                {
                    _context.Error(DiagnosticCodes.IncompatibleSpecificationParameterType, $"Parameter '{parameter.Name}' has unknown type '{parameter.Type.Name}'.", parameter.Type.Location);
                }
                foreach (var row in specification.Cases)
                {
                    foreach (var assignment in row.Values.Where(value => value.Property == parameter.Name && !values.Compatible(value.Source, parameter.Type)))
                    {
                        _context.Error(DiagnosticCodes.InvalidSpecificationCaseValue, $"Case '{row.Name}' parameter '{parameter.Name}' requires a concrete value of type '{parameter.Type.Name}'.", assignment.Source.Location);
                    }
                }
            }
            foreach (var (role, node, assignments) in CaseSteps(specification))
            {
                foreach (var assignment in assignments.Where(value => value.Source is CaseValueExpressionSyntax))
                {
                    var reference = (CaseValueExpressionSyntax)assignment.Source;
                    var parameter = specification.Parameters.FirstOrDefault(parameter => parameter.Name == reference.Parameter);
                    var target = ParameterTarget(specification, node, role, assignment, scope);
                    if (parameter is null || target is null) continue;
                    if (parameter.Type.IsOptional && !target.IsOptional)
                    {
                        _context.Error(DiagnosticCodes.OptionalSpecificationParameterTarget, $"Optional parameter '{parameter.Name}' cannot supply required target '{assignment.Property}'.", reference.Location);
                    }
                    var sourceConcept = _application.Concepts.SingleOrDefault(concept => concept.Name == parameter.Type.Name);
                    var targetConcept = _application.Concepts.SingleOrDefault(concept => concept.Name == target.Name);
                    var compatible = parameter.Type.Name == target.Name || (sourceConcept is { IsEnum: false } && sourceConcept.Type == target.Name) || (targetConcept is { IsEnum: false } && targetConcept.Type == parameter.Type.Name);
                    if (!compatible || parameter.Type.IsCollection != target.IsCollection)
                    {
                        _context.Error(DiagnosticCodes.IncompatibleSpecificationParameterType, $"Parameter '{parameter.Name}' type '{parameter.Type.Name}' is incompatible with target '{assignment.Property}' type '{target.Name}'.", reference.Location);
                    }
                }
            }
        }

        TypeRefSyntax? ParameterTarget(SpecificationSyntax specification, SyntaxNode node, string role, PropertyMappingSyntax assignment, DeclarationScope scope)
        {
            var property = assignment.Property;
            IEnumerable<PropertySyntax>? properties = null;
            switch (node)
            {
                case SpecificationCommandSyntax command:
                    var declaration = _declarations!.Resolve(command.CommandType, scope, slice => slice.Commands, item => item.Name)?.Node;
                    if (property == "for") return declaration?.Properties.SingleOrDefault(value => value.IsIdentifier)?.Type;
                    properties = declaration?.Properties;
                    property = property.StartsWith("generated ", StringComparison.Ordinal) ? property[10..] : property;
                    break;
                case SpecificationEventSyntax occurrence:
                    if (occurrence.Stream is { } compositeRoute && compositeRoute.StreamIdParts.Any(part => ReferenceEquals(part.Source, assignment.Source)))
                    {
                        var composite = new EventSourceCatalog(_application).Resolve(compositeRoute.EventSource, compositeRoute.Stream);
                        return composite.Kind == EventSourceResolutionKind.Unique ? composite.Streams[0].StreamIdParts.SingleOrDefault(part => part.Name == property)?.Type : null;
                    }
                    if (property == "streamId" && occurrence.Stream is { } route)
                    {
                        var stream = new EventSourceCatalog(_application).Resolve(route.EventSource, route.Stream);
                        return stream.Kind == EventSourceResolutionKind.Unique ? stream.Streams[0].StreamId : null;
                    }
                    if (property == "for")
                    {
                        if (occurrence.Stream is { } eventRoute)
                        {
                            var source = new EventSourceCatalog(_application).Resolve(eventRoute.EventSource, eventRoute.Stream);
                            if (source.Kind == EventSourceResolutionKind.Unique && source.Sources[0].Identifier is { } identifier) return identifier;
                        }
                        return specification.When is { } action ? _declarations!.Resolve(action.CommandType, scope, slice => slice.Commands, item => item.Name)?.Node.Properties.SingleOrDefault(value => value.IsIdentifier)?.Type : null;
                    }
                    properties = _declarations!.Event(occurrence.EventType, scope)?.Properties;
                    break;
                case SpecificationReadModelSyntax model:
                    properties = _declarations!.ViewProperties(model.Name, scope);
                    break;
                case SpecificationAbsentReadModelSyntax absent:
                    var absentModel = _declarations!.Resolve(absent.Name, scope, slice => slice.ReadModels ?? [], item => item.Name)?.Node;
                    var keyed = _slices.SelectMany(owner => owner.Slice.Queries.Where(query => query.By is not null && ReferenceEquals(_declarations.Resolve(query.ReturnType.Name, owner.Scope, slice => slice.ReadModels ?? [], item => item.Name)?.Node, absentModel))).ToArray();
                    return absentModel is not null && keyed.Length == 1 ? keyed[0].By!.Type : null;
                case SpecificationWhenQuerySyntax query:
                    properties = QueryProperties(query.Query, scope);
                    break;
                case SpecificationQuerySyntax query:
                    properties = QueryProperties(query.Query, scope);
                    break;
                case SpecificationQueryResultSyntax result:
                    var queryName = role == "then query result" ? specification.ThenQueries.First(query => query.Results.Any(candidate => ReferenceEquals(candidate, result))).Query : specification.WhenQuery?.Query;
                    var queryDeclaration = queryName is null ? null : _declarations!.Resolve(queryName, scope, slice => slice.Queries, item => item.Name);
                    properties = queryDeclaration is { } resolved ? _declarations!.ViewProperties(resolved.Node.ReturnType.Name, resolved.Scope) : null;
                    break;
                case SpecificationTriggerSyntax trigger:
                    return (_application.Triggers ?? []).SingleOrDefault(value => value.Name == trigger.Trigger)?.Data.SingleOrDefault(value => value.Name == property)?.Type;
                case SpecificationErrorSyntax:
                    return new("String", false, false, node.Location);
                case SpecificationReturnSyntax:
                    var actionCommand = specification.When is { } actionStep ? _declarations!.Resolve(actionStep.CommandType, scope, slice => slice.Commands, item => item.Name)?.Node : null;
                    return actionCommand?.Response switch
                    {
                        ScalarCommandResponseSyntax scalar => actionCommand.Properties.SingleOrDefault(item => item.Name == scalar.Source.Property)?.Type,
                        RecordCommandResponseSyntax record => record.Fields.SingleOrDefault(field => field.Name == property) is { } field
                            ? field.Type ?? actionCommand.Properties.SingleOrDefault(item => item.Name == field.Source.Property)?.Type : null,
                        _ => null
                    };
            }

            return _declarations!.Property(properties, property, out _)?.Type;
        }

        IEnumerable<PropertySyntax>? QueryProperties(string name, DeclarationScope scope)
        {
            var query = _declarations!.Resolve(name, scope, slice => slice.Queries, item => item.Name)?.Node;
            return query is null ? null : (query.By is null ? Enumerable.Empty<QueryParameterSyntax>() : [query.By]).Concat(query.Filters).Select(parameter => new PropertySyntax(parameter.Name, parameter.Type, parameter.Location));
        }
    }
}
