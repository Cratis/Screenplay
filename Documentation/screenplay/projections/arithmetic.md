---
title: "Screenplay arithmetic: sum event amounts"
description: "Use add and subtract in a Screenplay projection to accumulate event amounts in numeric read model properties; counters change them by one."
---

<a id="arithmetic-operations"></a>

Arithmetic in a Cratis Screenplay projection adds or subtracts an amount from a numeric read model property. Use it when an event carries a payment amount, stock quantity, or other value to accumulate. [Counters](counters) change a value by one per event, regardless of any amount it carries.

## Add

Use `add` to accumulate an amount into the property's current value:

```pdl
add {Property} by {expression}
```

<a id="example"></a>

In this mapping excerpt, each payment contributes its `amount` to `Balance`:

```pdl
from PaymentReceived
  add Balance by amount
  LastPayment = $eventContext.occurred
```

## Subtract

Use `subtract` when the event's amount should be taken from the current value:

```pdl
subtract {Property} by {expression}
```

<a id="example-1"></a>

A withdrawal changes the balance by its amount, not by the number of withdrawals:

```pdl
from WithdrawalMade
  subtract Balance by amount
  LastWithdrawal = $eventContext.occurred
```

## Expression Values

Use a numeric event property such as `amount` or `cost.total`, or a numeric literal such as `100` or `10.50`, after `by`. The target and operand must both be numeric. A fixed amount is useful when every occurrence has the same value; an event property preserves the amount recorded by each occurrence.

## Multiplying two values

A basket line records a unit price and a quantity. Multiply them after `by` to accumulate the line's value without adding a precomputed total to the event:

```screenplay
projection BasketProjection => Basket
  from LineAdded
    add total by unitPrice * quantity
```

With `unitPrice` of 19.99 and `quantity` of 3, this line contributes 59.97 to `total`. The product is calculated in decimal arithmetic, then converted to the target property's type before accumulation.

The operand form is `<value> * <value>`, with numeric values on both sides. Only one `*` is accepted, and only after `by` in `add` or `subtract`. A bare mapping such as `total = unitPrice * quantity` is not supported arithmetic. Multiplication selects language/semantic version `29.0`; models without it keep their existing version and bytes.

<a id="multiple-operations"></a>
<a id="examples"></a>
<a id="account-balance"></a>
<a id="loyalty-points"></a>
<a id="budget-tracking"></a>
<a id="order-totals"></a>
<a id="gaming-score"></a>

## Maintain a balance across deposits and withdrawals

This account excerpt starts from the opening balance. Later events add or subtract their own amounts:

```pdl
projection Account => AccountReadModel
  from AccountOpened
    AccountNumber = accountNumber
    Balance = initialBalance

  from DepositMade
    add Balance by amount
    LastDeposit = $eventContext.occurred

  from WithdrawalMade
    subtract Balance by amount
    LastWithdrawal = $eventContext.occurred

  from InterestApplied
    add Balance by interestAmount
```

Starting at 100, a deposit of 25 followed by a withdrawal of 10 leaves `Balance` at 115. Assigning `Balance = amount` would replace the balance with each transaction amount instead of accumulating it. See [Property Mapping](property-mapping.md) when replacement is the intended behavior.

<a id="inventory-with-variable-quantities"></a>

## Track stock quantities, not just stock events

One restock event can represent many items. The `quantity` operand makes that distinction explicit:

```pdl
projection Product => ProductReadModel
  from ProductCreated
    Name = name
    StockLevel = initialStock

  from StockAdded
    add StockLevel by quantity
    LastRestocked = $eventContext.occurred

  from StockSold
    subtract StockLevel by quantity

  from StockAdjusted
    add StockLevel by adjustmentAmount
```

`StockAdded` with `quantity = 12` adds 12, while a counter would add only 1. The adjustment handler accepts a signed amount: a positive `adjustmentAmount` raises stock and a negative one lowers it. Give each event an amount whose sign matches its meaning.

## Nested Properties

A dotted path reads an amount from a nested event property. It does not change how the amount is accumulated:

```pdl
from TransactionProcessed
  add Balance by transaction.amount
  add TotalTransactions by 1
```

## With Counters

To answer both “how much?” and “how often?”, update separate properties in the same handler:

```pdl
from SaleCompleted
  add TotalRevenue by saleAmount
  add TaxCollected by taxAmount
  increment SalesCount
  count CompletedTransactions
```

`TotalRevenue` and `TaxCollected` sum amounts. `SalesCount` and `CompletedTransactions` both increase by one for each `SaleCompleted` event; `count` and `increment` differ in wording, not execution.

## Property Requirements

Declare numeric targets, such as `Int`, `Decimal`, or concepts based on those types. The referenced event property must exist and have a numeric type too.

The reference evaluator seeds a missing numeric target with zero. When the initial state is supplied by an event, assign it explicitly, as `Balance = initialBalance` does above. For fractional amounts, choose `Decimal` or a decimal-based concept: a whole-number target rounds each operand to the nearest whole number, with midpoint ties rounded to even, before applying the operation.

## Error Handling

Semantic validation rejects non-numeric targets and operands. An operand that evaluates to null or a non-numeric value also fails during evaluation; it is not treated as zero.

<a id="best-practices"></a>

Arithmetic does not enforce domain rules such as a minimum balance. A subtraction can produce a negative result. Enforce such a rule in the behavior that decides whether the withdrawal or stock event can occur.

## Difference from Counters

| Operation | Amount per handled event | Use case |
|-----------|--------------------------|----------|
| `count` / `increment` | +1 | Number of occurrences or unit increases |
| `decrement` | −1 | Unit decreases |
| `add` / `subtract` | Supplied numeric amount | Balances, quantities, scores |

## See Also

Continue with [Counters](counters) for unit changes, [Property Mapping](property-mapping.md) for replacement values, or [Expressions](expressions) for the available value forms.
