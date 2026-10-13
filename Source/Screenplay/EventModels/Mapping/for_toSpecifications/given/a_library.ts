// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../../Document/EventModelDocument';
import { toEventModelDocument } from '../../EventModelDocumentVisitor';

// A small library: adding a book is a command, the shelf is a state view over two events, and lending a
// book sets off an automation. The state view and the automation are both specified by appending an event.
export const library_source = `
domain Library
module Library
  feature Catalog
    slice StateChange AddBook
      command AddBook
        bookId Uuid identifier
        title String
        produces BookAddedToCatalog
          for bookId
          title = title
      event BookAddedToCatalog
        title String
      specification AddingABook
        when AddBook
          bookId = "75926956-26c2-4e60-a239-10367a2afbd8"
          title = "The Quiet Archive"
        then BookAddedToCatalog
          title = "The Quiet Archive"
    slice StateChange LendBook
      command LendBook
        bookId Uuid identifier
        memberName String
        produces BookLent
          for bookId
          memberName = memberName
      event BookLent
        memberName String
    slice StateChange NotifyMember
      command NotifyMember
        bookId Uuid identifier
        produces MemberNotified
          for bookId
      event MemberNotified
    slice StateView AvailableBooks
      readmodel AvailableBook
        bookId Uuid
        title String
        onLoan Bool
      projection AvailableBooks => AvailableBook
        from BookAddedToCatalog
          bookId = $eventSourceId
          title = title
          onLoan = false
        from BookLent
          onLoan = true
      specification ShowingANewBookOnTheShelf
        when append BookAddedToCatalog
          for "33b45ad5-0114-474b-b348-0791336023f6"
          title = "The Quiet Archive"
        then readmodel AvailableBook
          bookId = "33b45ad5-0114-474b-b348-0791336023f6"
          onLoan = false
    slice Automation NotifyOnLoan
      reaction LoanNotifier
        when BookLent
          invokes NotifyMember
            bookId = $eventSourceId
      specification NotifyingTheMember
        when append BookLent
          for "8fd1cd22-9eb2-4f52-95b8-1c015b78feba"
          memberName = "Ola Brekke"
        then MemberNotified
`;

export function a_library(): EventModelDocument {
    const result = parse(library_source);
    if (result.diagnostics.length > 0) {
        throw new Error(`The library does not compile: ${result.diagnostics.map(diagnostic => diagnostic.message).join('; ')}`);
    }
    return toEventModelDocument(result.value, 'Library');
}
