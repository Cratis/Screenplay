// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Binds compatible source syntax to ESM v1 and reports every non-bound construct explicitly.
/// </summary>
public sealed class SemanticModelBinder : ISemanticModelBinder
{
    /// <inheritdoc/>
    public CompilationResult<SemanticCompilation> Bind(
        string applicationName,
        ApplicationSyntax syntax,
        SemanticDocumentSet documents)
    {
        var context = new BindingContext(applicationName, syntax, documents);
        try
        {
            var application = context.BindApplication();
            if (context.HasErrors)
            {
                return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics);
            }

            var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
            var sourceMap = SemanticSourceMap.Create(context.SourceMapEntries, documents.Documents);
            var compilation = SemanticCompilation.Create(model, documents, sourceMap);
            return new(compilation, context.Diagnostics);
        }
        catch (InvalidSemanticContract exception)
        {
            context.Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, syntax.Location);
            return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics);
        }
    }

    sealed class BindingContext(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents)
    {
        readonly List<Diagnostic> _diagnostics = [];
        readonly List<SemanticSourceMapEntry> _sourceMapEntries = [];
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _concepts = new(StringComparer.Ordinal);
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _types = new(StringComparer.Ordinal);
        readonly ApplicationIdentity _applicationIdentity = documents.IdentityCatalog.Application;

        internal IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        internal bool HasErrors => _diagnostics.Exists(_ => _.Severity == DiagnosticSeverity.Error);

        internal ImmutableArray<SemanticSourceMapEntry> SourceMapEntries => [.. _sourceMapEntries];

        internal SemanticApplication BindApplication()
        {
            ReportTopLevelDispositions();
            var applicationAddress = SemanticAddress.ForApplication(_applicationIdentity);
            var applicationId = Resolve(applicationAddress, syntax.Location);

            RegisterTypeDeclarations();
            var concepts = syntax.Concepts.Select(BindConcept).ToImmutableArray();
            var types = (syntax.Types ?? []).Select(BindType).ToImmutableArray();
            var modules = syntax.Modules.Select(BindModule).ToImmutableArray();
            return new(applicationId, applicationName, concepts, types, modules);
        }

        internal void Error(string code, string message, SourceLocation location) =>
            _diagnostics.Add(Diagnostic.Error(code, message, location));

        void Information(string code, string message, SourceLocation location) =>
            _diagnostics.Add(new(DiagnosticSeverity.Information, code, message, location));

        void ReportTopLevelDispositions()
        {
            if (syntax.Domain is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "A domain declaration is authoring metadata and is not part of ESM v1 behavior.", syntax.Domain.Location);
            }

            foreach (var import in syntax.Imports)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Import '{import.QualifiedName}' is not supported by ESM v1 binding.", import.Location);
            }

            foreach (var policy in syntax.Policies)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' is deferred until portable policy semantics are admitted.", policy.Location);
            }

            foreach (var persona in syntax.Personas ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Persona '{persona.Name}' is deferred from ESM v1.", persona.Location);
            }

            if (syntax.Authentication is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Authentication providers are realization metadata and are not part of ESM v1 behavior.", syntax.Authentication.Location);
            }

            foreach (var seed in syntax.Seeds ?? [])
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Event seeding is operational metadata and is not part of ESM v1 behavior.", seed.Location);
            }

            foreach (var trigger in syntax.Triggers ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Trigger '{trigger.Name}' is deferred until portable occurrence semantics are admitted.", trigger.Location);
            }

            foreach (var profile in syntax.UiProfiles ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"UI profile '{profile.Name}' is explicitly deferred from the backend ESM v1 profile.", profile.Location);
            }

            foreach (var theme in syntax.Themes ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Theme '{theme.Name}' is explicitly deferred from the backend ESM v1 profile.", theme.Location);
            }

            foreach (var layout in syntax.Layouts ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Layout '{layout.Name}' is explicitly deferred from the backend ESM v1 profile.", layout.Location);
            }
        }

        void RegisterTypeDeclarations()
        {
            foreach (var concept in syntax.Concepts)
            {
                var address = SemanticAddress.ForConcept(_applicationIdentity, concept.Name);
                _concepts[concept.Name] = (address, Resolve(address, concept.Location));
            }

            foreach (var type in syntax.Types ?? [])
            {
                var address = SemanticAddress.ForCompositeType(_applicationIdentity, type.Name);
                _types[type.Name] = (address, Resolve(address, type.Location));
            }
        }

        SemanticConcept BindConcept(ConceptSyntax concept)
        {
            if (concept.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Concept '{concept.Name}' file reference is realization provenance.", concept.File.Location);
            }

            if (concept.Attributes.Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' compliance attributes require portable data-subject semantics.", concept.Location);
            }

            if ((concept.Validations ?? []).Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' validation binding is not admitted by the current ESM v1 subset.", concept.Location);
            }

            var primitive = concept.IsEnum ? SemanticPrimitiveType.Text : Primitive(concept.Type);
            return new(_concepts[concept.Name].Id, concept.Name, primitive, [.. concept.Values], []);
        }

        SemanticCompositeType BindType(TypeSyntax type)
        {
            if (type.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Composite type '{type.Name}' description is authoring metadata.", type.Location);
            }

            if (type.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Composite type '{type.Name}' file reference is realization provenance.", type.File.Location);
            }

            var owner = _types[type.Name].Address;
            var properties = type.Properties.Select(property => BindProperty(owner, property, false)).ToImmutableArray();
            return new(_types[type.Name].Id, type.Name, properties);
        }

        SemanticModule BindModule(ModuleSyntax module)
        {
            if (module.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Module '{module.Name}' description is authoring metadata.", module.Location);
            }

            foreach (var template in module.ScreenTemplates)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Screen template '{template.Name}' is explicitly deferred from the backend ESM v1 profile.", template.Location);
            }

            foreach (var template in module.DialogTemplates ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Dialog template '{template.Name}' is explicitly deferred from the backend ESM v1 profile.", template.Location);
            }

            foreach (var form in module.Forms ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Form '{form.Name}' is explicitly deferred from the backend ESM v1 profile.", form.Location);
            }

            foreach (var contribution in module.Contributions ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Contribution to '{contribution.ContributionPoint}' is explicitly deferred from the backend ESM v1 profile.", contribution.Location);
            }

            var address = SemanticAddress.ForModule(_applicationIdentity, module.Name);
            var id = Resolve(address, module.Location);
            var features = module.Features.Select(feature => BindFeature(module.Name, [], feature)).ToImmutableArray();
            return new(id, module.Name, features);
        }

        SemanticFeature BindFeature(string module, ImmutableArray<string> parentPath, FeatureSyntax feature)
        {
            if (feature.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Feature '{feature.Name}' description is authoring metadata.", feature.Location);
            }

            foreach (var contribution in feature.Contributions ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Contribution to '{contribution.ContributionPoint}' is explicitly deferred from the backend ESM v1 profile.", contribution.Location);
            }

            var path = parentPath.Add(feature.Name);
            var address = SemanticAddress.ForFeature(_applicationIdentity, module, path);
            var id = Resolve(address, feature.Location);
            var nested = feature.Features.Select(value => BindFeature(module, path, value)).ToImmutableArray();
            var slices = feature.Slices.Select(value => BindSlice(module, path, value)).ToImmutableArray();
            return new(id, feature.Name, nested, slices);
        }

        SemanticSlice BindSlice(string module, ImmutableArray<string> featurePath, SliceSyntax slice)
        {
            if (slice.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Slice '{slice.Name}' description is authoring metadata.", slice.Location);
            }

            if (slice.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Slice '{slice.Name}' file reference is realization provenance.", slice.File.Location);
            }

            var kind = slice.Type switch
            {
                SliceType.StateChange => SemanticSliceKind.StateChange,
                SliceType.StateView => SemanticSliceKind.StateView,
                _ => SemanticSliceKind.Unknown
            };
            if (kind == SemanticSliceKind.Unknown)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Slice '{slice.Name}' of type '{slice.Type}' is not admitted by ESM v1.", slice.Location);
            }

            var address = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
            var id = Resolve(address, slice.Location);
            var events = slice.Events.Select(value => BindEvent(address, value)).ToArray();
            var eventsByName = events.ToDictionary(value => value.Syntax.Name, StringComparer.Ordinal);
            var commands = slice.Commands.Select(value => BindCommand(address, value, eventsByName)).ToImmutableArray();
            ReportUnsupportedSliceMembers(slice);
            return new(id, slice.Name, kind, [.. events.Select(_ => _.Contract)], commands, [], [], [], []);
        }

        BoundEvent BindEvent(SemanticAddress slice, EventSyntax @event)
        {
            if (@event.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Event '{@event.Name}' file reference is realization provenance.", @event.File.Location);
            }

            if ((@event.Tags ?? []).Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Event '{@event.Name}' tags are not admitted by ESM v1.", @event.Location);
            }

            var address = SemanticAddress.ForEventContract(slice, @event.Name);
            var semanticAssignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            var contractAssignment = documents.IdentityCatalog.ResolveEventContract(address);
            Map(semanticAssignment.Id, contractAssignment.Origin, @event.Location);
            var properties = @event.Properties.Select(property => BindProperty(address, property, false)).ToImmutableArray();
            return new(
                @event,
                new(
                    semanticAssignment.Id,
                    contractAssignment.Id,
                    contractAssignment.Revision,
                    @event.Name,
                    properties),
                properties.ToDictionary(_ => _.Name, StringComparer.Ordinal));
        }

        SemanticCommand BindCommand(
            SemanticAddress slice,
            CommandSyntax command,
            Dictionary<string, BoundEvent> events)
        {
            if (command.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Command '{command.Name}' description is authoring metadata.", command.Location);
            }

            if (command.Authorize is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' authorization requires portable policy semantics.", command.Authorize.Location);
            }

            foreach (var reads in command.Reads ?? [])
            {
                Error(
                    DiagnosticCodes.PreservedLegacySemanticSyntax,
                    $"Command '{command.Name}' reads '{reads.ReadModel}' with legacy semantics that cannot imply decision consistency.",
                    reads.Location);
            }

            if (command.Concurrency is not null)
            {
                Error(
                    DiagnosticCodes.PreservedLegacySemanticSyntax,
                    $"Command '{command.Name}' concurrency metadata keeps its legacy meaning and cannot bind to ESM v1.",
                    command.Concurrency.Location);
            }

            if (command.Handler is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' handler requires a constrained implementation attachment.", command.Handler.Location);
            }

            var address = SemanticAddress.ForCommand(slice, command.Name);
            var id = Resolve(address, command.Location);
            var properties = command.Properties.Select(property => BindProperty(address, property, property.IsIdentifier)).ToImmutableArray();
            var propertiesByName = properties.ToDictionary(_ => _.Name, StringComparer.Ordinal);
            var validations = BindValidations(command, propertiesByName);
            var produced = command.Produces
                .Select(value => BindProducedEvent(command, value, propertiesByName, events))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            return new(id, command.Name, properties, validations, produced);
        }

        ImmutableArray<SemanticValidationRule> BindValidations(
            CommandSyntax command,
            Dictionary<string, SemanticProperty> properties)
        {
            var validations = ImmutableArray.CreateBuilder<SemanticValidationRule>();
            foreach (var validation in command.Validations)
            {
                if (validation is not DeclarativeValidateSyntax declarative)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' code validation requires a constrained implementation attachment.", validation.Location);
                    continue;
                }

                foreach (var requirement in declarative.Requirements ?? [])
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' requirement conditions are not admitted by the first ESM v1 vertical.", requirement.Location);
                }

                foreach (var rule in declarative.Rules)
                {
                    if (rule.Rule != ValidationRuleKind.NotEmpty || rule.Value is not null || rule.File is not null || rule.Code is not null)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Validation rule '{rule.Rule}' on '{rule.Property}' is not admitted by the first ESM v1 vertical.", rule.Location);
                        continue;
                    }

                    if (!properties.TryGetValue(rule.Property, out var property))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Validation rule property '{rule.Property}' is unresolved on command '{command.Name}'.", rule.Location);
                        continue;
                    }

                    validations.Add(new(property.Id, SemanticValidationRuleKind.NotEmpty, null, rule.Message));
                }
            }

            return validations.ToImmutable();
        }

        SemanticProducedEvent? BindProducedEvent(
            CommandSyntax command,
            ProducesSyntax produced,
            Dictionary<string, SemanticProperty> commandProperties,
            Dictionary<string, BoundEvent> events)
        {
            if (!events.TryGetValue(produced.Event, out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Produced event '{produced.Event}' is not declared in slice '{command.Name}'.", produced.Location);
                return null;
            }

            if (produced.When is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Conditional production of '{produced.Event}' is not admitted by the first ESM v1 vertical.", produced.When.Location);
            }

            if ((produced.Tags ?? []).Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Produced event '{produced.Event}' tags are not admitted by ESM v1.", produced.Location);
            }

            var destination = produced.For is null
                ? null
                : BindPropertyExpression(produced.For, commandProperties, "produced event destination");
            var mappings = ImmutableArray.CreateBuilder<SemanticPropertyMapping>();
            foreach (var mapping in produced.Mappings)
            {
                if (!@event.Properties.TryGetValue(mapping.Property, out var target))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Produced event mapping target '{mapping.Property}' is unresolved on '{@event.Syntax.Name}'.", mapping.Location);
                    continue;
                }

                if (BindExpression(mapping.Source, commandProperties, "produced event mapping") is { } source)
                {
                    mappings.Add(new(target.Id, source));
                }
            }

            return new(@event.Contract.Id, null, destination, mappings.ToImmutable());
        }

        SemanticExpression? BindPropertyExpression(
            ExpressionSyntax expression,
            Dictionary<string, SemanticProperty> properties,
            string description)
        {
            if (expression is PathExpressionSyntax path && properties.TryGetValue(path.Path, out var property))
            {
                return SemanticExpression.Property(SemanticExpressionRootKind.Command, property.Id);
            }

            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"A {description} must resolve to one command property in ESM v1.", expression.Location);
            return null;
        }

        SemanticExpression? BindExpression(
            ExpressionSyntax expression,
            Dictionary<string, SemanticProperty> properties,
            string description) => expression switch
        {
            PathExpressionSyntax path when properties.TryGetValue(path.Path, out var property) =>
                SemanticExpression.Property(SemanticExpressionRootKind.Command, property.Id),
            LiteralExpressionSyntax literal => SemanticExpression.FromValue(BindLiteral(literal)),
            _ => UnsupportedExpression(expression, description)
        };

        SemanticValue BindLiteral(LiteralExpressionSyntax expression) => expression.Value switch
        {
            null => SemanticValue.Null,
            string value => SemanticValue.Text(value),
            bool value => SemanticValue.Boolean(value),
            double value => SemanticValue.Number(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
            _ => throw new InvalidSemanticContract($"Literal value type '{expression.Value.GetType().Name}' is unsupported during semantic binding.")
        };

        SemanticExpression? UnsupportedExpression(ExpressionSyntax expression, string description)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"The {description} expression '{expression.GetType().Name}' is not admitted by ESM v1.", expression.Location);
            return null;
        }

        void ReportUnsupportedSliceMembers(SliceSyntax slice)
        {
            foreach (var readModel in slice.ReadModels ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Read model '{readModel.Name}' binding is not implemented in the current ESM increment.", readModel.Location);
            }

            foreach (var projection in slice.Projections)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' binding is not implemented in the current ESM increment.", projection.Location);
            }

            foreach (var query in slice.Queries)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' binding is not implemented in the current ESM increment.", query.Location);
            }

            foreach (var specification in slice.Specifications)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification '{specification.Name}' binding is not implemented in the current ESM increment.", specification.Location);
            }

            foreach (var reducer in slice.Reducers ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reducer '{reducer.Name}' requires a portable reducer contract.", reducer.Location);
            }

            foreach (var reaction in slice.Reactions)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' requires portable occurrence and effect semantics.", reaction.Location);
            }

            foreach (var capture in slice.Captures)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture.Name}' requires a portable compiled CDL plan.", capture.Location);
            }

            foreach (var constraint in slice.Constraints)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Constraint '{constraint.Name}' requires a portable constraint contract.", constraint.Location);
            }

            foreach (var screen in slice.Screens)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Screen '{screen.Name}' is explicitly deferred from the backend ESM v1 profile.", screen.Location);
            }
        }

        SemanticProperty BindProperty(SemanticAddress owner, PropertySyntax property, bool isIdentifier)
        {
            var address = SemanticAddress.ForProperty(owner, property.Name);
            var id = Resolve(address, property.Location);
            return new(id, property.Name, BindTypeReference(property.Type), isIdentifier);
        }

        SemanticTypeReference BindTypeReference(TypeRefSyntax type) => type.Name switch
        {
            "Uuid" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid, type.IsCollection, type.IsOptional),
            "String" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, type.IsCollection, type.IsOptional),
            "Int" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber, type.IsCollection, type.IsOptional),
            "Decimal" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber, type.IsCollection, type.IsOptional),
            "Bool" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean, type.IsCollection, type.IsOptional),
            "Date" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Date, type.IsCollection, type.IsOptional),
            "DateTime" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime, type.IsCollection, type.IsOptional),
            _ when _concepts.TryGetValue(type.Name, out var concept) => SemanticTypeReference.ForConcept(concept.Id, type.IsCollection, type.IsOptional),
            _ when _types.TryGetValue(type.Name, out var composite) => SemanticTypeReference.ForCompositeType(composite.Id, type.IsCollection, type.IsOptional),
            _ => throw new InvalidSemanticContract($"Type reference '{type.Name}' is unresolved during semantic binding.")
        };

        SemanticPrimitiveType Primitive(string value) => value switch
        {
            "Uuid" => SemanticPrimitiveType.Uuid,
            "String" => SemanticPrimitiveType.Text,
            "Int" => SemanticPrimitiveType.WholeNumber,
            "Decimal" => SemanticPrimitiveType.DecimalNumber,
            "Bool" => SemanticPrimitiveType.Boolean,
            "Date" => SemanticPrimitiveType.Date,
            "DateTime" => SemanticPrimitiveType.DateTime,
            _ => throw new InvalidSemanticContract($"Primitive type '{value}' is unsupported during semantic binding.")
        };

        SemanticId Resolve(SemanticAddress address, SourceLocation location)
        {
            var assignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            Map(assignment.Id, assignment.Origin, location);
            return assignment.Id;
        }

        void Map(SemanticId id, SemanticIdentityOrigin origin, SourceLocation location)
        {
            if (DocumentAt(location) is not { } document)
            {
                return;
            }

            try
            {
                var offset = OffsetAt(document.Text, location);
                var span = SemanticSourceSpan.Create(document.Id, offset, 0, location.Line, location.Column, location.Line, location.Column);
                _sourceMapEntries.Add(new(id, span, origin));
            }
            catch (InvalidSemanticContract exception)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, location);
            }
        }

        SemanticSourceDocument? DocumentAt(SourceLocation location)
        {
            if (location.Path is null && documents.Documents.Length == 1)
            {
                return documents.Documents[0];
            }

            var document = documents.Documents.FirstOrDefault(value =>
                string.Equals(value.DisplayPath, location.Path, StringComparison.OrdinalIgnoreCase));
            if (document is null)
            {
                Error(
                    DiagnosticCodes.UnknownSemanticSourceDocument,
                    $"Source location path '{location.Path ?? "<none>"}' does not identify one supplied semantic document.",
                    location);
            }

            return document;
        }

        int OffsetAt(string text, SourceLocation location)
        {
            var line = 1;
            var offset = 0;
            while (line < location.Line && offset < text.Length)
            {
                if (text[offset] == '\r')
                {
                    offset++;
                    if (offset < text.Length && text[offset] == '\n')
                    {
                        offset++;
                    }

                    line++;
                }
                else if (text[offset++] == '\n')
                {
                    line++;
                }
            }

            if (line != location.Line)
            {
                throw new InvalidSemanticContract("A semantic syntax location is outside its source document.");
            }

            var lineStart = offset;
            while (offset < text.Length && text[offset] is not ('\r' or '\n'))
            {
                offset++;
            }

            var lineLength = offset - lineStart;
            if (location.Column > lineLength + 1)
            {
                throw new InvalidSemanticContract("A semantic syntax column is outside its source line.");
            }

            return lineStart + location.Column - 1;
        }

        sealed record BoundEvent(
            EventSyntax Syntax,
            SemanticEventContract Contract,
            Dictionary<string, SemanticProperty> Properties);
    }
}
