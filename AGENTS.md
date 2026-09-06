# Repository Guidelines

## Project Overview

`Telegram.Bot.Advanced` is a `net10.0`/C# 14 NuGet library that adds filter-based Telegram update dispatch, hosted-lifecycle polling/webhook transports, EF Core chat state, and optional async newsletters. `Telegram.Bot.Advanced.TestServer` is a runnable ASP.NET Core manual/integration host—not an automated test project. `Telegram.Bot.Advanced.Tests` is the automated xUnit v3 (native Microsoft.Testing.Platform) regression suite.

## Architecture & Data Flow

1. Hosts configure `TelegramBotData`/`TelegramBotDataOptions` (construction performs no network I/O), build or supply an `IDispatcher`, then call `services.AddTelegramHolder(...)`.
2. Register exactly one transport with `services.AddTelegramPolling()` or `services.AddTelegramWebhooks(options => { ... })`; webhook mode additionally needs `app.MapTelegramWebhooks()`. Both transports run as `IHostedService`s that resolve the bot's `GetMe` username in `StartAsync` before producing `Telegram.Bot.Types.Update` instances for `Dispatcher<TContext>`.
3. The dispatcher creates its own `IServiceProvider.CreateAsyncScope()` per update, loads or creates `TelegramChat` state through the host-provided `TelegramContext`, persists pre-handler changes, evaluates controller and method `DispatcherFilterAttribute`s against an immutable `HandlerDescriptor[]`, then invokes the first matching handler (or the single `[NoMethodFilter]` fallback).
4. Controllers receive `Update`, `MessageCommand`, `TelegramContext`, `TelegramChat`, bot data, and a per-update `CancellationToken` through `ITelegramController<TContext>`. Newsletter delivery is an optional scoped `INewsletterService` (fully async) over the same EF entities, exposed through `NewsletterSubscriptionController<TContext>` and `NewsletterPublishingController<TContext>`.

Preserve this boundary: host code owns the concrete EF context/provider and bot configuration; the library owns dispatch/filter behavior, transport lifecycle, and its base entities.

## Key Directories

- `Telegram.Bot.Advanced/Core/Dispatcher/` — dispatcher, `HandlerDescriptor`, builder/interfaces, and filter attributes; the main routing path.
- `Telegram.Bot.Advanced/Core/Holder/` — bot configuration, endpoint lookup (`ITelegramHolder.TryGet`), and options.
- `Telegram.Bot.Advanced/Core/Hosting/` — `IHostedService` implementations for polling, webhooks, and startup newsletters.
- `Telegram.Bot.Advanced/Core/Middlewares/` — webhook request-to-update handler, including secret-token authentication.
- `Telegram.Bot.Advanced/Controller/` — controller contracts/base classes, including the split newsletter controllers.
- `Telegram.Bot.Advanced/DbContexts/` — `TelegramContext` and framework chat/newsletter entities.
- `Telegram.Bot.Advanced/Extensions/` — host-facing `IServiceCollection`/`IEndpointRouteBuilder` extensions.
- `Telegram.Bot.Advanced/Services/` — scoped async newsletter service and registration.
- `Telegram.Bot.Advanced.TestServer/` — manual polling/webhook exercise host, in-memory context, handlers, and launch profiles.
- `Telegram.Bot.Advanced.Tests/` — automated regression suite (native MTP via `dotnet test`).
- `.github/workflows/publish.yml` — sole CI workflow; restore, build, test, tag-version gate, pack, then OIDC (`NuGet/login`) publish on `v*` tags.

## Development Commands

Use a .NET 10 SDK; `global.json` selects the native Microsoft.Testing.Platform test runner.

```sh
dotnet restore Telegram.Bot.Advanced.slnx
dotnet build Telegram.Bot.Advanced.slnx --configuration Release --no-restore
dotnet test Telegram.Bot.Advanced.Tests/Telegram.Bot.Advanced.Tests.csproj --configuration Release --no-build
```

The library has `GeneratePackageOnBuild=true`, so its package is produced during a normal build. `TreatWarningsAsErrors` is enabled on every project; a warning fails the build.

Manual TestServer smoke paths require a real Telegram token and may contact external Telegram/webhook services:

```sh
dotnet user-secrets set "BotToken" "<token>" --project Telegram.Bot.Advanced.TestServer
dotnet run --project Telegram.Bot.Advanced.TestServer -- polling
dotnet run --project Telegram.Bot.Advanced.TestServer -- webhook
```

`polling` is the default mode. Webhook mode additionally requires `Telegram:BaseUrl` (appsettings) and `Telegram:WebhookSecret` (user secrets/environment — never checked in). Do not treat these commands as deterministic automated tests.

## Code Conventions & Common Patterns

- Follow the edited file's existing namespace form and Allman-brace style; most files use block namespaces, with a few file-scoped exceptions.
- Public APIs use PascalCase; interfaces use `I` prefixes; private fields use `_camelCase`; the generic EF context parameter is `TContext`.
- Nullable references and implicit usings are enabled on every project. Annotate optional references with `?`; validate required setup at public construction/registration boundaries.
- New host integration belongs in extension methods on `IServiceCollection` or `IEndpointRouteBuilder`. Keep holder lifetime singleton and controllers/newsletter services scoped.
- Configure new bots through `new TelegramBotData(options => { ... })`; `TelegramBotDataBuilder` no longer exists.
- Routable controller handlers must be declared public, instance, parameterless methods on an `ITelegramController<TContext>` implementation, and must return `void`, `Task`, or `ValueTask` — the dispatcher rejects other return types when building its `HandlerDescriptor[]`. Use existing `DispatcherFilterAttribute` subclasses on controller types/methods; filters are conjunctive and `[NoMethodFilter]` is the (single, optional) fallback.
- Use `async`/`await` for every EF Core and Telegram I/O call; there are no synchronous alternatives left in the library. Thread the controller's `CancellationToken` (or the dispatcher/hosted-service token) through every call. Dispatcher failures are logged then rethrown (webhook dispatch failures surface as HTTP 500 so Telegram retries); newsletter fan-out records per-chat send errors in the immutable `SendResult`.
- Preserve `TelegramContext` keys/relationships and update persistence deliberately. The dispatcher saves chat/sender changes before handler selection and fetches `ChatFullInfo` only when a `TelegramChat` is first created.
- There is no update de-duplication in the dispatcher or webhook middleware by design: a failed dispatch must remain retryable. Do not reintroduce last-update-ID tracking.

## Important Files

- `Telegram.Bot.Advanced/Extensions/ServiceCollectionExtensions.cs` — DI, transport, and startup-newsletter registration entry point.
- `Telegram.Bot.Advanced/Extensions/ApplicationBuilderExtensions.cs` — `MapTelegramWebhooks` endpoint registration.
- `Telegram.Bot.Advanced/Core/Dispatcher/Dispatcher.cs` and `HandlerDescriptor.cs` — scopes, state, filter selection, reflection invocation, and error behavior.
- `Telegram.Bot.Advanced/Core/Holder/TelegramBotData.cs` and `TelegramBotDataOptions.cs` — primary bot setup contract; construction performs no network I/O.
- `Telegram.Bot.Advanced/Core/Hosting/` — `TelegramPollingHostedService`, `TelegramWebhookHostedService`, `StartupNewsletterHostedService`.
- `Telegram.Bot.Advanced/Core/Middlewares/TelegramRouting.cs` — webhook secret-token authentication (fixed-time comparison) and update dispatch.
- `Telegram.Bot.Advanced/Core/Dispatcher/Filters/DispatcherFilter.cs` — base filter contract; follow sibling filter patterns.
- `Telegram.Bot.Advanced/DbContexts/TelegramContext.cs` and `TelegramChat.cs` — persistence model and chat-data helpers.
- `Telegram.Bot.Advanced/Telegram.Bot.Advanced.csproj` — authoritative target/dependencies/package metadata and the single `<Version>`.
- `Telegram.Bot.Advanced.TestServer/Program.cs` and `Properties/launchSettings.json` — TestServer startup/mode configuration.
- `Telegram.Bot.Advanced.Tests/` — automated regression suite; add new coverage here.

## Runtime/Tooling Preferences

- Use `dotnet` and SDK-style `PackageReference`; there is no Node, Docker, Compose, package-lock, central package-management, or checked-in NuGet-source configuration.
- Do not add generated `bin/`, `obj/`, `*.nupkg`, IDE files, or local `nuget.config`; they are ignored.
- Preserve normal text line-ending behavior (`.gitattributes` has `* text=auto`).
- C# language intelligence is configured in `.omp/lsp.json` with `csharp-ls`; use LSP navigation/references/rename instead of text-only cross-file C# refactors.
- TestServer reads `BotToken` from user secrets and `Telegram:BaseUrl`/`Telegram:Webhook` from `appsettings.json`; `Telegram:WebhookSecret` must come from user secrets or environment, never a checked-in file.

## Testing & QA

`Telegram.Bot.Advanced.Tests` is the automated regression suite: xUnit v3 on the native Microsoft.Testing.Platform runner (see `global.json`), using NSubstitute for library-owned interfaces and a queued `HttpMessageHandler` behind real `TelegramBotClient` instances for Telegram API calls — no outbound network and no TestServer dependency. Run it with `dotnet test Telegram.Bot.Advanced.Tests/Telegram.Bot.Advanced.Tests.csproj`.

For behavior that also benefits from live verification, use the manual TestServer only when valid Telegram credentials and the relevant external environment are available. Useful existing scenarios are the polling `help`, `async`, and `setup` commands, and the webhook `help`, `command`, no-command echo, and chat-state transitions in `Telegram.Bot.Advanced.TestServer/TelegramController/`.

Before changing dispatch behavior, prefer extending `Telegram.Bot.Advanced.Tests` over manual verification: filter matching/fallback, command parsing, cancellation propagation, scoped context handling, persistence ordering, and newsletter per-recipient error aggregation are all covered there and should stay covered after a change.
