// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayPrinter"/> that renders a Screenplay syntax tree
/// back to <c>.play</c> source text.
/// </summary>
/// <remarks>
/// The printer walks the tree recursively, tracking indentation with a <see cref="ScreenplayWriter"/> and
/// inverting the exact keyword and expression forms the parsers accept. It also implements the syntax
/// visitor interfaces so it composes with <see cref="IScreenplayCompiler"/>'s visitor overloads.
/// </remarks>
public sealed partial class ScreenplayPrinter :
    IScreenplayPrinter,
    IApplicationSyntaxVisitor<string>,
    IProjectionSyntaxVisitor<string>,
    ISpecificationSyntaxVisitor<string>,
    ICaptureSyntaxVisitor<string>
{
    /// <inheritdoc/>
    public string Print(ApplicationSyntax application)
    {
        var writer = new ScreenplayWriter();
        WriteApplication(writer, application);
        return writer.ToString();
    }

    /// <inheritdoc/>
    public string Print(ProjectionSyntax projection)
    {
        var writer = new ScreenplayWriter();
        WriteProjection(writer, projection);
        return writer.ToString();
    }

    /// <inheritdoc/>
    public string Print(SpecificationSyntax specification)
    {
        var writer = new ScreenplayWriter();
        WriteSpecification(writer, specification);
        return writer.ToString();
    }

    /// <inheritdoc/>
    public string Print(CaptureSyntax capture)
    {
        var writer = new ScreenplayWriter();
        WriteCapture(writer, capture);
        return writer.ToString();
    }

    /// <inheritdoc/>
    string IApplicationSyntaxVisitor<string>.Visit(ApplicationSyntax syntax) => Print(syntax);

    /// <inheritdoc/>
    string IProjectionSyntaxVisitor<string>.Visit(ProjectionSyntax syntax) => Print(syntax);

    /// <inheritdoc/>
    string ISpecificationSyntaxVisitor<string>.Visit(SpecificationSyntax syntax) => Print(syntax);

    /// <inheritdoc/>
    string ICaptureSyntaxVisitor<string>.Visit(CaptureSyntax syntax) => Print(syntax);

    void WriteApplication(ScreenplayWriter writer, ApplicationSyntax application)
    {
        if (application.Domain is not null)
        {
            writer.Line($"domain {application.Domain.Name}");
            writer.Blank();
        }

        foreach (var import in application.Imports)
        {
            writer.Line($"import {import.QualifiedName}");
        }

        foreach (var concept in application.Concepts)
        {
            writer.Blank();
            WriteConcept(writer, concept);
        }

        foreach (var policy in application.Policies)
        {
            writer.Blank();
            WritePolicy(writer, policy);
        }

        foreach (var module in application.Modules)
        {
            writer.Blank();
            WriteModule(writer, module);
        }
    }

    void WriteConcept(ScreenplayWriter writer, ConceptSyntax concept)
    {
        var attributes = concept.Attributes.Select(attribute => $" @{attribute}");
        writer.Line($"concept {concept.Name} : {concept.Type}{string.Concat(attributes)}");
        if (!concept.IsEnum)
        {
            return;
        }

        using (writer.Indent())
        {
            foreach (var value in concept.Values)
            {
                writer.Line(value);
            }
        }
    }

    void WritePolicy(ScreenplayWriter writer, PolicySyntax policy)
    {
        writer.Line($"policy {policy.Name}");
        using (writer.Indent())
        {
            if (policy.Condition is not null)
            {
                writer.Line($"require {ScreenplaySyntaxText.PolicyCondition(policy.Condition)}");
            }

            if (policy.Code is not null)
            {
                WriteCodeBlock(writer, policy.Code);
            }
        }
    }

    void WriteModule(ScreenplayWriter writer, ModuleSyntax module)
    {
        writer.Line($"module {module.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, module.Description);

            foreach (var layout in module.Layouts)
            {
                writer.Blank();
                WriteLayout(writer, layout);
            }

            foreach (var feature in module.Features)
            {
                writer.Blank();
                WriteFeature(writer, feature);
            }
        }
    }

    void WriteLayout(ScreenplayWriter writer, LayoutSyntax layout)
    {
        writer.Line($"layout {layout.Name}");
        using (writer.Indent())
        {
            writer.Line("template");
            using (writer.Indent())
            {
                foreach (var slot in layout.Slots)
                {
                    writer.Line(slot);
                }
            }
        }
    }

    void WriteFeature(ScreenplayWriter writer, FeatureSyntax feature)
    {
        writer.Line($"feature {feature.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, feature.Description);

            foreach (var nested in feature.Features)
            {
                writer.Blank();
                WriteFeature(writer, nested);
            }

            foreach (var slice in feature.Slices)
            {
                writer.Blank();
                WriteSlice(writer, slice);
            }
        }
    }

    void WriteSlice(ScreenplayWriter writer, SliceSyntax slice)
    {
        writer.Line($"slice {slice.Type} {slice.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, slice.Description);

            foreach (var command in slice.Commands)
            {
                writer.Blank();
                WriteCommand(writer, command);
            }

            foreach (var @event in slice.Events)
            {
                writer.Blank();
                WriteEvent(writer, @event);
            }

            foreach (var constraint in slice.Constraints)
            {
                writer.Blank();
                WriteConstraint(writer, constraint);
            }

            foreach (var query in slice.Queries)
            {
                writer.Blank();
                WriteQuery(writer, query);
            }

            if (slice.Projection is not null)
            {
                writer.Blank();
                WriteProjection(writer, slice.Projection);
            }

            foreach (var capture in slice.Captures)
            {
                writer.Blank();
                WriteCapture(writer, capture);
            }

            foreach (var reactor in slice.Reactors)
            {
                writer.Blank();
                WriteReactor(writer, reactor);
            }

            foreach (var screen in slice.Screens)
            {
                writer.Blank();
                WriteScreen(writer, screen);
            }

            foreach (var specification in slice.Specifications)
            {
                writer.Blank();
                WriteSpecification(writer, specification);
            }
        }
    }

    void WriteDescription(ScreenplayWriter writer, string? description)
    {
        if (description is not null)
        {
            writer.Line($"description \"{description}\"");
        }
    }
}
