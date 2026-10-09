// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    void WritePurpose(ScreenplayWriter writer, PurposeSyntax purpose)
    {
        using var anchor = writer.Anchor(purpose);
        writer.Line($"purpose {purpose.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, purpose.Description, purpose);
            WritePurposeVocabulary(writer, purpose, "basis", purpose.Basis, purpose.BasisReference);
            WritePurposeText(writer, purpose, "interest", purpose.Interest);
            WritePurposeVocabulary(writer, purpose, "condition", purpose.Condition, purpose.ConditionReference);
            WritePurposeText(writer, purpose, "authorization", purpose.Authorization);
            if (purpose.Subjects.Any()) writer.DirectiveLine($"subjects {string.Join(", ", purpose.Subjects)}", purpose, "subjects");
            WritePurposeText(writer, purpose, "retention", purpose.Retention);
            var recipients = purpose.Recipients.ToArray();
            for (var index = 0; index < recipients.Length; index++) writer.DirectiveLine($"recipient {StringLiteral.Quote(recipients[index])}", purpose, DirectiveLocationKeys.ForValue("recipient", recipients, index));
            foreach (var transfer in purpose.Transfers) writer.Line($"transfer {StringLiteral.Quote(transfer.Destination)} safeguard {StringLiteral.Quote(transfer.Safeguard)}", transfer);
            if (purpose.ErasureException is not null) writer.DirectiveLine($"erasure exception {purpose.ErasureException}", purpose, "erasure");
        }
    }

    void WritePurposeVocabulary(ScreenplayWriter writer, PurposeSyntax purpose, string field, string? value, string? reference)
    {
        if (value is not null) writer.DirectiveLine($"{field} {value}{(reference is null ? string.Empty : " " + StringLiteral.Quote(reference))}", purpose, field);
    }

    void WritePurposeText(ScreenplayWriter writer, PurposeSyntax purpose, string field, string? value)
    {
        if (value is not null) writer.DirectiveLine($"{field} {StringLiteral.Quote(value)}", purpose, field);
    }
}
