# Glossary

## Account

A place where money is held, such as Cash, Bank, or E-Wallet.

## API

Application Programming Interface. The HTTP interface used by the mobile app and web dashboard to communicate with the backend.

## Backend

Server-side application responsible for validation, business rules, authentication, synchronization, and persistence.

## Balance

The calculated amount of money currently held in an account based on applicable transactions.

## Blazor

The .NET web UI framework used for the laptop dashboard and inside the .NET MAUI mobile application through Blazor Hybrid.

## Blazor Hybrid

A .NET MAUI approach where Razor/Blazor components run inside a native application WebView while sharing .NET application code with the native app.

## Budget

A planned spending limit for a category and time period.

## Category

A label used to classify income or expense, such as Food, Transport, or Salary.

## Docker Compose

A declarative way to run the project's related containers as one deployment stack.

## Expense

A transaction representing money leaving an account for spending.

## Income

A transaction representing money entering an account as income.

## Offline-first

An application design where core user actions continue to work without network connectivity.

## Operation ID

A unique identifier for a synchronization operation. It allows a server to safely process the same client request more than once without creating duplicates.

## PostgreSQL

The server-side relational database and canonical source of truth.

## PWA

Progressive Web App. Not the primary mobile strategy for this project; the mobile application uses .NET MAUI + Blazor Hybrid instead.

## SQLite

The local embedded relational database stored on the mobile device.

## Sync Queue

A local list of changes waiting to be uploaded to the server.

## Source of Truth

The authoritative copy of data used to establish the correct state. PostgreSQL is the server source of truth.

## Transfer

A movement of money between two accounts. A transfer is not income and not expense.

## VPS

Virtual Private Server. The cloud server used to host the backend, PostgreSQL, and supporting services.

## Worker

A background process responsible for non-request work such as exports, cleanup, or future synchronization tasks.
