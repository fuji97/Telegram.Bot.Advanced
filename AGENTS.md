# Repository Guidelines

## Project Overview

`Telegram.Bot.Advanced` is a `net6.0`/C# 10 NuGet library that adds filter-based Telegram update dispatch, ASP.NET Core webhook or polling integration, EF Core chat state, and optional newsletters. `Telegram.Bot.Advanced.TestServer` is a runnable ASP.NET Core manual/integration host—not an automated test project.

## Architecture & Data Flow

1. Hosts configure `TelegramBotData`/`TelegramBotDataOptions`, build or supply an `IDispatcher`, then call `services.AddTelegramHolder(...)`.
2. Use `app.UseTelegramRouting(...)` for webhooks or `app.UseTelegramPolling()` for polling. Both produce `Telegram.Bot.Types.Update` instances for `Dispatcher<TContext>`.
3. The dispatcher creates a DI scope, loads or creates `TelegramChat` state through the host-provided `TelegramContext`, persists pre-handler changes, evaluates controller and method `DispatcherFilterAttribute`s, then invokes the first matching handler.
4. Controllers receive `Update`, `MessageCommand`, `TelegramContext`, `TelegramChat`, and bot data through `ITelegramController<TContext>`. Newsletter delivery is an optional scoped service over the same EF entities.

Preserve this boundary: host code owns the concrete EF context/provider and bot configuration; the library owns dispatch/filter behavior and its base entities.

## Key Directories

- `Telegram.Bot.Advanced/Core/Dispatcher/` — dispatcher, builder/interfaces, and filter attributes; the main routing path.
- `Telegram.Bot.Advanced/Core/Holder/` — bot configuration, endpoint lookup, and options.
- `Telegram.Bot.Advanced/Core/Middlewares/` — webhook request-to-update adapter.
- `Telegram.Bot.Advanced/Controller/` — controller contracts/base classes.
- `Telegram.Bot.Advanced/DbContexts/` — `TelegramContext` and framework chat/newsletter entities.
- `Telegram.Bot.Advanced/Extensions/` — host-facing `IServiceCollection`/`IApplicationBuilder` extensions.
- `Telegram.Bot.Advanced/Services/` — scoped newsletter service and registration.
- `Telegram.Bot.Advanced.TestServer/` — manual polling/webhook exercise host, in-memory context, handlers, and launch profiles.
- `.github/workflows/publish.yml` — sole CI workflow; restore, release build, then NuGet publishing on `master`.

## Development Commands

Use a .NET 6 SDK; CI installs `6.x` and no `global.json` pins a different SDK.

```sh
dotnet restore
dotnet build --configuration Release --no-restore
```

The library has `GeneratePackageOnBuild=true`, so its package is produced during a normal build.

Manual TestServer smoke paths require a real Telegram token and may contact external Telegram/webhook services:

```sh
dotnet user-secrets set "BotToken" "<token>" --project Telegram.Bot.Advanced.TestServer
dotnet run --project Telegram.Bot.Advanced.TestServer -- polling
dotnet run --project Telegram.Bot.Advanced.TestServer -- webhook
```

`polling` is the default mode. Do not treat these commands as deterministic automated tests.

## Code Conventions & Common Patterns

- Follow the edited file's existing namespace form and Allman-brace style; most files use block namespaces, with a few file-scoped exceptions.
- Public APIs use PascalCase; interfaces use `I` prefixes; private fields use `_camelCase`; the generic EF context parameter is `TContext`.
- Nullable references are enabled in the library. Annotate optional references with `?`; validate required setup at public construction/registration boundaries.
- New host integration belongs in extension methods on `IServiceCollection` or `IApplicationBuilder`. Keep holder lifetime singleton and controllers/newsletter services scoped.
- Configure new bots through `new TelegramBotData(options => { ... })`; do not introduce new use of obsolete `TelegramBotDataBuilder` APIs.
- Routable controller handlers must be declared public, instance, parameterless methods on an `ITelegramController<TContext>` implementation. Use existing `DispatcherFilterAttribute` subclasses on controller types/methods; filters are conjunctive and `[NoMethodFilter]` is the fallback.
- Prefer `async`/`await` for EF Core and Telegram I/O. Preserve established error semantics: dispatcher failures are logged then rethrown; newsletter fan-out records per-chat send errors in `SendResult`.
- Preserve `TelegramContext` keys/relationships and update persistence deliberately. The dispatcher saves chat/sender changes before handler selection.
- Update de-duplication/state is mutable and unsynchronized in dispatcher/middleware paths. Audit both paths before changing concurrency or duplicate-update logic.

## Important Files

- `Telegram.Bot.Advanced/Extensions/ServiceCollectionExtensions.cs` — DI registration entry point.
- `Telegram.Bot.Advanced/Extensions/ApplicationBuilderExtensions.cs` — webhook, polling, and startup-newsletter host entry points.
- `Telegram.Bot.Advanced/Core/Dispatcher/Dispatcher.cs` — scopes, state, filter selection, reflection invocation, and error behavior.
- `Telegram.Bot.Advanced/Core/Holder/TelegramBotData.cs` and `TelegramBotDataOptions.cs` — primary bot setup contract.
- `Telegram.Bot.Advanced/Core/Dispatcher/Filters/DispatcherFilter.cs` — base filter contract; follow sibling filter patterns.
- `Telegram.Bot.Advanced/DbContexts/TelegramContext.cs` and `TelegramChat.cs` — persistence model and chat-data helpers.
- `Telegram.Bot.Advanced/Telegram.Bot.Advanced.csproj` — authoritative target/dependencies/package metadata.
- `Telegram.Bot.Advanced/Telegram.Bot.Advanced.nuspec` — separate legacy-looking package metadata with dependency versions that differ from the project; review both for packaging changes.
- `Telegram.Bot.Advanced.TestServer/Program.cs` and `Properties/launchSettings.json` — TestServer startup/mode configuration.

## Runtime/Tooling Preferences

- Use `dotnet` and SDK-style `PackageReference`; there is no Node, Docker, Compose, package-lock, central package-management, or checked-in NuGet-source configuration.
- Do not add generated `bin/`, `obj/`, `*.nupkg`, IDE files, or local `nuget.config`; they are ignored.
- Preserve normal text line-ending behavior (`.gitattributes` has `* text=auto`).
- C# language intelligence is configured in `.omp/lsp.json` with `csharp-ls`; use LSP navigation/references/rename instead of text-only cross-file C# refactors.
- TestServer reads `BotToken` from user secrets. Its checked-in webhook settings need review before depending on webhook mode: `Program.cs` reads a root `BaseUrl`, while `appsettings.json` defines `Telegram:BaseUrl`.
- Treat `Telegram.Bot.Advanced.TestServer/Properties/PublishProfiles/*.pubxml` as legacy deployment artifacts: they declare `netcoreapp2.2`, unlike the active `net6.0` project.

## Testing & QA

No automated test project, test framework, coverage setup, test command, formatter, linter, or CI test step is checked in. Do not invent one or describe `TestServer` as a unit-test suite.

For behavior that requires validation, use the manual TestServer only when valid Telegram credentials and the relevant external environment are available. Useful existing scenarios are the polling `help`, `async`, and `setup` commands, and the webhook `help`, `command`, no-command echo, and chat-state transitions in `Telegram.Bot.Advanced.TestServer/TelegramController/`.

Before changing dispatch behavior, prioritize a focused reproduction of filter matching/fallback, command parsing, update de-duplication, scoped context handling, persistence ordering, or newsletter per-recipient error aggregation. Add a durable automated test only when it protects an observable contract and can run without external Telegram infrastructure.
