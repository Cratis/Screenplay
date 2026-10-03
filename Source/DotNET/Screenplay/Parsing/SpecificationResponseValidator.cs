// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates generated fixtures, request boundaries and response assertions using known syntax shapes.
/// </summary>
internal static class SpecificationResponseValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var index = new CommandIndex(application, declarations);
        var values = new ResponseValueTypes(application);
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var specification in slice.Specifications)
            {
                var command = specification.When is { } when ? index.Resolve(when.CommandType, scope) : null;
                if (specification.When is { } action && command is not null)
                {
                    RejectInputs(action.Values.Select(value => (value.Property, value.Location)), index.Generated(command), context);
                    var names = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var fixture in action.GeneratedValues)
                    {
                        if (!names.Add(fixture.Property) || !index.Properties(command).TryGetValue(fixture.Property, out var property) || !property.IsGenerated || property.IsIdentifier || !values.Compatible(fixture.Source, property.Type))
                        {
                            context.Error(DiagnosticCodes.InvalidGeneratedFixture, "A generated fixture must uniquely supply a compatible value for a nonidentifier generated command property.", fixture.Location);
                        }
                    }
                }

                if (specification.ThenReturns is not { } expectation) continue;
                if (specification.When is null || specification.ThenErrors.Any() || specification.ThenDenied is not null)
                {
                    context.Error(DiagnosticCodes.InvalidReturnExpectation, "A return expectation requires a command action and cannot accompany an error or denial.", expectation.Location);
                }

                if (command is null) continue; // Unknown/imported shapes are handled by existing reference checks.
                var properties = index.Properties(command);
                if (expectation is ScalarSpecificationReturnSyntax scalar && command.Response is ScalarCommandResponseSyntax response && properties.TryGetValue(response.Source.Property, out var source))
                {
                    if (!values.Compatible(scalar.Value, source.Type)) context.Error(DiagnosticCodes.InvalidReturnExpectation, "The expected return value must match the response source's type.", scalar.Location);
                }
                else if (expectation is RecordSpecificationReturnSyntax record && command.Response is RecordCommandResponseSyntax)
                {
                    var fields = index.ResponseFields(command);
                    var asserted = new HashSet<string>(StringComparer.Ordinal);
                    var assertions = record.Fields.ToArray();
                    if (assertions.Length == 0) context.Error(DiagnosticCodes.InvalidReturnExpectation, "A record return expectation requires at least one field.", record.Location);
                    foreach (var field in assertions)
                    {
                        if (!asserted.Add(field.Property) || !fields.TryGetValue(field.Property, out var declared) || !properties.TryGetValue(declared.Source.Property, out var property) || !values.Compatible(field.Source, property.Type))
                        {
                            context.Error(DiagnosticCodes.InvalidReturnExpectation, "A return assertion must uniquely name a response field and supply a compatible concrete value.", field.Location);
                        }
                    }
                }
                else
                {
                    context.Error(DiagnosticCodes.InvalidReturnExpectation, "The return expectation must match the command's scalar or record response shape.", expectation.Location);
                }
            }
        }

        new InputWalker(index, context).VisitApplication(application);
    }

    static void RejectInputs(IEnumerable<(string Name, SourceLocation Location)> inputs, HashSet<string> generated, ParserContext context)
    {
        foreach (var input in inputs)
        {
            if (generated.Contains(input.Name.Split('.')[0])) context.Error(DiagnosticCodes.GeneratedPropertySuppliedAsInput, $"Generated property '{input.Name}' cannot be supplied as request or form input.", input.Location);
        }
    }

    sealed class CommandIndex
    {
        readonly ILookup<string, (CommandSyntax Node, Declaration Declaration)> _commands;
        readonly ILookup<string, ImportSyntax> _imports;
        readonly Dictionary<CommandSyntax, Dictionary<string, PropertySyntax>> _properties;
        readonly Dictionary<CommandSyntax, HashSet<string>> _generated;
        readonly Dictionary<CommandSyntax, Dictionary<string, ResponseFieldSyntax>> _responseFields;
        readonly Dictionary<(string Name, string Scope), CommandSyntax?> _resolved = [];

        internal CommandIndex(ApplicationSyntax application, ConsistencyDeclarations declarations)
        {
            _commands = declarations.Slices.SelectMany(entry => entry.Slice.Commands.Select(command => (Node: command, Declaration: new Declaration(command.Name, entry.Scope)))).ToLookup(entry => entry.Declaration.Name, StringComparer.Ordinal);
            _imports = application.Imports.ToLookup(import => import.Name, StringComparer.Ordinal);
            _properties = _commands.SelectMany(group => group).Select(entry => entry.Node).Distinct().ToDictionary(command => command, command => command.Properties.GroupBy(property => property.Name, StringComparer.Ordinal).Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal));
            _generated = _properties.Keys.ToDictionary(command => command, command => command.Properties.Where(property => property.IsGenerated).Select(property => property.Name).ToHashSet(StringComparer.Ordinal));
            _responseFields = _properties.Keys.ToDictionary(command => command, command => (command.Response is RecordCommandResponseSyntax response ? response.Fields : []).GroupBy(field => field.Name, StringComparer.Ordinal).Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal));
        }

        internal Dictionary<string, PropertySyntax> Properties(CommandSyntax command) => _properties[command];
        internal HashSet<string> Generated(CommandSyntax command) => _generated[command];
        internal Dictionary<string, ResponseFieldSyntax> ResponseFields(CommandSyntax command) => _responseFields[command];

        internal CommandSyntax? Resolve(string name, DeclarationScope scope)
        {
            var key = (name, string.Join('.', scope.Segments));
            if (_resolved.TryGetValue(key, out var cached)) return cached;
            (CommandSyntax Node, Declaration Declaration)[] entries = [.. _commands[name.Split('.')[^1]]];
            var resolution = ReferenceResolver.Resolve(name, scope, [.. entries.Select(entry => entry.Declaration)]);
            ImportSyntax[] imports = [.. _imports[name]];
            if (resolution.IsUnresolved && imports is [var imported])
            {
                entries = [.. _commands[imported.QualifiedName.Split('.')[^1]]];
                resolution = ReferenceResolver.Resolve(imported.QualifiedName, scope, [.. entries.Select(entry => entry.Declaration)]);
            }

            var command = resolution.Resolved is { } resolved ? entries.First(entry => entry.Declaration == resolved).Node : null;
            _resolved[key] = command;

            return command;
        }
    }

    sealed class InputWalker(CommandIndex index, ParserContext context) : ScreenplaySyntaxWalker
    {
        DeclarationScope _scope = new([]);
        HashSet<string> _parameters = [];

        public override void VisitModule(ModuleSyntax syntax)
        {
            var previous = _scope;
            _scope = new([syntax.Name]);
            base.VisitModule(syntax);
            _scope = previous;
        }

        public override void VisitFeature(FeatureSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitFeature(syntax);
            _scope = previous;
        }

        public override void VisitSlice(SliceSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitSlice(syntax);
            _scope = previous;
        }

        public override void VisitBehavior(BehaviorSyntax syntax)
        {
            var previous = _parameters;
            _parameters = syntax.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
            base.VisitBehavior(syntax);
            _parameters = previous;
        }

        public override void VisitForm(FormSyntax syntax)
        {
            if (index.Resolve(syntax.For, _scope) is { } command) RejectInputs(syntax.Fields.Select(field => (field.Property, field.Location)), index.Generated(command), context);
            base.VisitForm(syntax);
        }

        public override void VisitInvokes(InvokesSyntax syntax)
        {
            if (index.Resolve(syntax.Command, _scope) is { } command) RejectInputs(syntax.Mappings.Select(mapping => (mapping.Property, mapping.Location)), index.Generated(command), context);
            base.VisitInvokes(syntax);
        }

        public override void VisitExecuteCommandAction(ExecuteCommandActionSyntax syntax)
        {
            if (!_parameters.Contains(syntax.Command) && index.Resolve(syntax.Command, _scope) is { } command) RejectInputs(syntax.Arguments.Select(argument => (argument.Name, argument.Location)), index.Generated(command), context);
            base.VisitExecuteCommandAction(syntax);
        }
    }
}
