# UI Specification

## 1. Design principles

The interface is divided by device role:

- **Phone:** fast input and practical daily management.
- **Laptop:** monitoring, visualization, review, and reporting.

The laptop does not need to reproduce the mobile quick-entry workflow.

## 2. Mobile application

Technology:

```text
.NET MAUI
+ Blazor Hybrid
+ SQLite
```

### 2.1 Home

Show:

- total/current balances by account;
- today's income/expense summary;
- quick action to add transaction;
- sync status.

Primary action should be visually dominant:

```text
+ Add Transaction
```

### 2.2 Add Transaction

Preferred first version:

```text
Type: Expense | Income | Transfer
Amount
Category (when applicable)
Account
Date
Note (optional)

[Save]
```

The form should optimize for speed rather than maximum configurability.

### 2.3 Transactions

Show:

- date;
- description/category;
- account;
- amount;
- sync state where relevant.

Filtering should initially support:

- date range;
- type;
- account;
- category.

### 2.4 Accounts

Show each account and derived balance.

Actions:

- add;
- edit;
- archive.

### 2.5 Categories

Show categories grouped simply.

Actions:

- add;
- edit;
- archive.

### 2.6 Budget

Show category budget progress:

```text
Food
Rp 900.000 / Rp 1.500.000
60%
```

Avoid accounting terminology that is not necessary for the user.

### 2.7 Settings

At minimum:

- account/session settings;
- sync status/details;
- manual sync/retry;
- export trigger if exposed on mobile;
- app version.

## 3. Laptop dashboard

Technology:

```text
ASP.NET Core
+ Blazor Web
```

### 3.1 Dashboard

Recommended layout:

```text
-------------------------------------------------
| Income | Expense | Net Change | Total Balance |
-------------------------------------------------
|                                               |
| Expense by Category       Recent Transactions |
|                                               |
| Budget Progress                              |
-------------------------------------------------
```

### 3.2 Reports

Initial reports:

- monthly income vs expense;
- expense by category;
- account balances;
- transaction history;
- budget vs actual.

### 3.3 Transactions page

Laptop users should be able to inspect and correct transactions even though laptop is not the primary entry device.

Editing must respect the same server business rules as mobile.

## 4. Responsive behavior

The web application should remain usable on smaller screens, but it is not required to replace the native mobile application.

## 5. Empty states

Examples:

```text
No transactions yet.
Add your first transaction from the phone.
```

```text
No budget configured for this category.
```

## 6. Error states

Errors must be understandable and actionable.

Prefer:

```text
Couldn't sync 1 transaction.
We'll retry automatically.
[Retry now]
```

over:

```text
HTTP 500 System.InvalidOperationException...
```

## 7. Visual direction

The UI should be:

- calm;
- readable;
- practical;
- low-clutter;
- mobile-first for daily entry;
- data-dense but understandable on desktop.

Do not introduce a design system dependency unless it materially improves development speed or accessibility.
