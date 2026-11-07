// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Works out which form fields are pickers - <c>choose from query &lt;Query&gt; show &lt;field&gt;</c> - whether written
/// or inferred, and checks the written ones.
/// </summary>
/// <remarks>
/// A field is inferred to be a picker when its command property's type is the key of a read model exactly one
/// list query returns. It shows the read model's <c>name</c> or <c>title</c> field, or else its first field that is
/// not the key.
/// </remarks>
public static class FormFieldChoices
{
    /// <summary>
    /// Gets the choices of a form field - the written <c>choose from query</c> line, or the inferred one.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> the form belongs to.</param>
    /// <param name="form">The <see cref="FormSyntax"/> holding the field.</param>
    /// <param name="field">The <see cref="FormFieldSyntax"/>.</param>
    /// <returns>The <see cref="FormFieldChoicesSyntax"/>, or <c>null</c> when the field is not a picker.</returns>
    public static FormFieldChoicesSyntax? Resolve(ApplicationSyntax application, FormSyntax form, FormFieldSyntax field)
    {
        if (field.Choices is not null)
        {
            return field.Choices;
        }

        var slices = ToolbarButtonValidator.AllSlices(application).ConvertAll(entry => entry.Slice);
        var property = Property(slices, form, field);
        if (property is null)
        {
            return null;
        }

        var keyed = slices.SelectMany(slice => slice.ReadModels ?? [])
            .Where(model => model.Properties.Any(candidate => candidate.IsKey && candidate.Type.Name == property.Type.Name))
            .ToList();
        var lists = slices.SelectMany(slice => slice.Queries)
            .Where(query => query.ReturnType.IsCollection && keyed.Exists(model => model.Name == query.ReturnType.Name))
            .ToList();
        if (lists.Count != 1)
        {
            return null;
        }

        var model = keyed.First(candidate => candidate.Name == lists[0].ReturnType.Name);
        var show = model.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, "name", StringComparison.Ordinal) || string.Equals(candidate.Name, "title", StringComparison.Ordinal))
            ?? model.Properties.FirstOrDefault(candidate => !candidate.IsKey)
            ?? model.Properties.First();
        return new(lists[0].Name, show.Name, field.Location);
    }

    /// <summary>
    /// Checks every written <c>choose from query</c> line of an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to check.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var slices = ToolbarButtonValidator.AllSlices(application).ConvertAll(entry => entry.Slice);
        foreach (var form in application.Modules.SelectMany(module => module.Forms ?? []))
        {
            foreach (var field in form.Fields)
            {
                if (field.Choices is { } choices)
                {
                    Check(slices, field, choices, context);
                }
            }
        }
    }

    static void Check(List<SliceSyntax> slices, FormFieldSyntax field, FormFieldChoicesSyntax choices, ParserContext context)
    {
        var name = choices.Query.Split('.')[^1];
        var query = slices.SelectMany(slice => slice.Queries).FirstOrDefault(candidate => candidate.Name == name);
        if (query is null)
        {
            context.Error(DiagnosticCodes.UnknownQuery, $"Field '{field.Property}' chooses from query '{choices.Query}', which the document does not declare", choices.Location);
            return;
        }

        if (!query.ReturnType.IsCollection)
        {
            context.Error(DiagnosticCodes.InvalidFormFieldChoices, $"Field '{field.Property}' chooses from query '{choices.Query}', which returns one result - a picker chooses from a list query", choices.Location);
            return;
        }

        var model = slices.SelectMany(slice => slice.ReadModels ?? []).FirstOrDefault(candidate => candidate.Name == query.ReturnType.Name);
        if (model?.Properties.Any(property => property.Name == choices.Show) == false)
        {
            context.Error(DiagnosticCodes.InvalidFormFieldChoices, $"Field '{field.Property}' shows '{choices.Show}', but the rows of query '{choices.Query}' ({model.Name}) have no field '{choices.Show}'", choices.Location);
        }
    }

    static PropertySyntax? Property(List<SliceSyntax> slices, FormSyntax form, FormFieldSyntax field)
    {
        var commandName = form.For.Split('.')[^1];
        var command = slices.SelectMany(slice => slice.Commands).FirstOrDefault(candidate => candidate.Name == commandName);
        var propertyName = field.Property.Split('.')[0];
        return command?.Properties.FirstOrDefault(candidate => candidate.Name == propertyName);
    }
}
