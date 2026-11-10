# NxtShift · نكست شفت

A work-from-home shift timer, shipped as a **Blazor WebAssembly PWA** on GitHub Pages.
Punch in with one tap, watch the shift count down, and get reminded to punch out on the work system.
Arabic and English (RTL-aware), light and dark, works offline. Part of the Nxt family; built on
[Nxt.UI](https://github.com/RamiAlkhateeb/Nxt.UI) (git submodule at `lib/Nxt.UI`).

## Layout

| Project | What lives there |
|---|---|
| `NxtShift.Core` | Pure logic: `ShiftMath` (time arithmetic, which screen to show, upcoming reminders), `ShiftStore` (state and every transition, persisted as one JSON value). |
| `NxtShift.Tests` | xUnit tests for everything in Core. |
| `NxtShift.Web` | The WASM host: pages (Welcome, Home, Running, TimeUp, Done, Settings), services (strings, reminders, device), `wwwroot` (CSS, icons, manifest, service worker). |
| `tools/prepare-pages.py` | Turns `dotnet publish` output into a Pages site under `/<repo>/`. |

```bash
git submodule update --init
dotnet build NxtShift.slnx
dotnet test NxtShift.Tests
dotnet run --project NxtShift.Web
```

## How reminders work (no backend)

- **Sounds** (`nxt.chime` in Nxt.UI): a short tick each time another full hour of the shift is done, and a longer
  rising tune when the shift ends (ticks again on each repeat). Both can be switched off, and Settings has a
  "Test sounds" button. Audio unlocks on the first tap in the app.
- **Notifications** (`nxt.notify`): a heads-up 10 minutes before the end, one at the end, then every 5 minutes until
  "I've punched out" (up to 12 times).

Both only work while the app is alive. On Android that means it's open or was used recently; on iPhone it must stay on
screen, since iOS freezes web apps in the background. A static web app can't wake a closed phone (there's no push
server). Missed hour marks are not played late, and reopening the app always lands on the right screen.

Everything on screen is derived from timestamps (epoch ms), so the countdown is right after the phone sleeps, across
midnight, and across DST changes. State lives in `localStorage` under `nxtshift.state`.

## Deploying

`.github/workflows/pages.yml` tests, publishes, prepares and deploys on every push to `main`.
One-time setup: **Settings › Pages › Source: GitHub Actions**. The app is then at
`https://ramialkhateeb.github.io/NxtShift/`.

Bump `NxtShift.Web/wwwroot/version.json` on a release so installed apps show the "new version" banner.
