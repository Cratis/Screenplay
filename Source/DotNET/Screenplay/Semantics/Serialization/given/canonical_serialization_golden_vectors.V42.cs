// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v42 golden: every v41 construct plus screens admitted as a stack-neutral UI contract.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v42 golden.
    /// </summary>
    public const string ScreensSource = GraphForEachSource + """

        module Desk
          feature Tickets
            slice StateChange OpenTicket
              command OpenTicket
                ticketId String identifier
                subject  String
                board    String

                produces TicketOpened
                  for ticketId
                  subject = subject

              event TicketOpened
                subject String

              screen OpenTicketScreen
                fits dialog, page
                description "Open a ticket for a customer"
                title "Open a ticket"
                section Details
                  title "What is it about?"
                  action OpenTicket
                    label "Open"
                    with board from "support"
                    navigate to TicketBoard

            slice StateView TicketBoard
              readmodel Ticket
                ticketId String key
                subject  String

              query AllTickets => Ticket[]

              projection TicketBoard => Ticket
                from TicketOpened
                  ticketId = $eventSourceId
                  subject  = subject

              screen TicketBoard
                title "Tickets"
                data Ticket[] via query AllTickets
                table Ticket
                  column subject label "Subject"
                action OpenTicket for screen
                navigate to OpenTicketScreen
        """;

    /// <summary>
    /// Gets the canonical bytes of the v42 golden.
    /// </summary>
    public static byte[] EsmV42Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v42.json");

    /// <summary>
    /// Creates the v42 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV42()
    {
        var result = property_order_binding.AdmitScreens(ScreensSource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif
