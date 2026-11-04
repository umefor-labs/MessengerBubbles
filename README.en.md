# Messenger Bubbles

English | [Tiếng Việt](README.md)

A Windows 11 desktop app that shows Facebook chats as floating bubbles, one bubble per conversation, similar to chat heads on Android. Built with WPF and WebView2, it wraps `https://www.facebook.com/messages`. This is a personal project and is not affiliated with Meta.

The documents in `docs/` are in Vietnamese.

![Intro video](docs/media/messenger-bubbles-intro.gif)

A 49-second intro video, in Vietnamese, recreating the interface with made-up data. The image above is silent. The version with the notification sound is the [MP4 file](https://github.com/umefor-labs/MessengerBubbles/raw/main/docs/media/messenger-bubbles-intro.mp4), which GitHub downloads rather than plays. The source is in `promo/`, and `python promo/render.py` exports it again.

Latest version: `v0.6.1`. The status of each milestone is in the [roadmap](docs/ROADMAP.md).

## Risks

The app reads Facebook's web interface because there is no public API:

- If Facebook changes its page structure, message detection or the chat panel can break. See the [checklist](docs/FACEBOOK-CHECKLIST.md).
- [Facebook's terms](https://www.facebook.com/terms.php) prohibit access by automated means without permission. The app reads the chat list at least once every 5 seconds, so your account could be restricted. Full notes are in [PUBLISHING.md](docs/PUBLISHING.md).

## Requirements

| Component | Requirement |
|---|---|
| Operating system | Windows |
| WebView2 Runtime | Required, included with Windows 11. Without it the app shows an error and exits |
| .NET | Not needed for the release file. Building from source needs the .NET 10 SDK |

## Install and run

Download `MessengerBubbles-<tag>-win-x64.exe` from GitHub Releases and run it. The file has no digital signature, so SmartScreen may warn you.

On first run a login window appears. Log in to Facebook, enter your encrypted messages PIN if asked, then close the window. Closing it only hides it. To quit the app, choose `Quit` in the tray menu.

## Language

The app comes in Vietnamese and English. On first run it asks for a language before doing anything else. If you close that window without choosing, it picks Vietnamese when Windows is in Vietnamese, and English otherwise. Change it later under `Language` in the tray menu, which applies at once. This covers the app's own text only. Inside the chat panel, Facebook uses your account's language.

## Tray menu

| Item | Default | Effect |
|---|---|---|
| `Open Facebook` | | Shows the watcher page window. Double-clicking the icon does the same |
| `Persistent bubble` | Off | A fixed bubble that opens the chat list and shows the number of unread chats |
| `Group bubbles` | Off | Two or more bubbles stack. A click spreads them, and they regroup after 5 seconds |
| `Preview messages` | On | Shows a card with the name and message beside the bubble for 5 seconds |
| `Sound` | On | The app's own sound, at least 3 seconds apart |
| `Chat panel size` | Medium | Small 340×480, Medium 380×560, Large 460×720 (DIP). Applies from the next time the panel opens |
| `Language` | Asked on first run | Tiếng Việt or English |
| `Notify about updates` | On | Asks GitHub for the latest version, 1 minute after starting and every 24 hours. If there is a newer one it shows a notice, and clicking it opens the download page. The app never downloads or installs anything itself |
| `Start with Windows` | Off | Writes the value `MessengerBubbles` to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. While on, the app rewrites its own path each time it opens, so after moving the file, opening it once is enough |
| `Quit` | | Quits the app |

The tray icon turns red when you need to log in again or when reading the chat list fails.

## Architecture

```text
                     facebook.com/messages
                      |                 |
          Watcher (hidden WebView2)  ChatPanel (WebView2, suspended when hidden)
          injects probe.js            injects panel.js
                      |                 ^
                  chat rows             | opens a conversation
                      v                 |
          NewMessageDetector            |
                      |                 |
                 ScanResult             |
                      v                 |
          BubbleController ------- bubble click
             |        |       |
         BubbleSet  BubbleWindow  PreviewWindow, DismissZoneWindow
```

The solution has three projects:

| Project | Contents |
|---|---|
| `src/MessengerBubbles.Core` | Logic with no UI dependency: message detection, bubble set, geometry, settings, autostart, texts in both languages (`Texts.cs`) |
| `src/MessengerBubbles` | WPF, WinForms `NotifyIcon`, WebView2, embedded resources |
| `tests/MessengerBubbles.Tests` | xUnit, tests the Core project only |

| File | Role |
|---|---|
| `Program.cs` | Entry point, `MessengerBubbles.SingleInstance` mutex, wires the components together |
| `Watcher.cs` | Hidden WebView2 that stays on the list page, receives data from `probe.js`, tracks login and health, reads avatars |
| `ChatPanel.cs` | The chat panel, preloaded and suspended while hidden |
| `BubbleController.cs` | Applies `ScanResult` to bubble windows, sound, preview, grouping |
| `BubbleWindow.cs` | One bubble: avatar, count, drag and drop, animation |
| `FloatingWindow.cs` | Base class for floating windows: physical pixel positions, never takes focus |
| `OverlayWindows.cs` | Dismiss zone and preview card |
| `TrayIcon.cs` | Tray icon, menu, error notices |
| `UpdateChecker.cs` | Asks GitHub for the latest version |
| `LanguageWindow.cs` | Language window on first run |
| `AppPaths.cs` | Paths, WebView2 environment, logging, security restrictions |
| `Assets/probe.js` | Reads chat rows in the watcher page |
| `Assets/panel.js` | CSS and control functions in the panel page |

Both WebView2 controls use the same profile folder and options, so they share one browser process and one login session.

## Message detection

`probe.js` finds every `a[href*="/messages/t/"]` and `a[href*="/messages/e2ee/t/"]` and sends them to the app through `chrome.webview.postMessage`. Each row has:

| Field | Source |
|---|---|
| `href` | The `href` attribute of the `a` element |
| `text` | The row's `innerText` |
| `bold` | Whether a `span` has a `font-weight` of 600 or more |
| `avatar` | `src` of `img` or `xlink:href` of `image` |
| `muted` | Whether the crossed-out bell icon is there, found by a `path` whose `d` starts with `M2.5 6c0-.322` |

Sending runs after DOM changes, batched over 500 ms, and also every 5 seconds. The app ignores data from any page that is not `facebook.com` over HTTPS.

`NewMessageDetector.Scan` compares each row with the previous scan, after `RowText` has removed the time and status labels:

| Result | Condition |
|---|---|
| New message | Settling time has passed, the row is bold, has no `muted`, and (the row is unseen, or it was not bold before, or its text changed) |
| Read | The row was bold and no longer is |

Settling time is 10 seconds from the first rendered row, because Facebook builds the list gradually after loading. State resets each time the watcher page navigates. Removing the labels means a changed "Active now" label or a ticking time does not count as a new message.

Messages for the conversation showing in the panel are filtered in `BubbleController.Apply`, through `window.__cbShowing`.

## Chat panel

The panel page is preloaded off-screen at startup, waits up to 40 seconds for chat rows, then is suspended with `CoreWebView2.TrySuspendAsync`. A bubble click resumes it at once, without a reload. A suspended page runs nothing, so a conversation left open cannot mark incoming messages as read. This is why opening the app does not change unread state.

Opening a conversation goes through functions that `panel.js` attaches to `window`:

| Function | Effect |
|---|---|
| `__cbOpen(href)` | Clicks the chat row in the page. Returns `clicked`, `pushed`, `wait` or `off-page` |
| `__cbCurrent()` | Path of the conversation actually showing, or `null` |
| `__cbShowing(href)` | Whether conversation `href` is showing |
| `__cbHasRows()` | Whether chat rows have rendered |
| `__cbFocus()` | Moves the cursor to the message box |

Clicking a row routes inside the page, because opening the URL directly at a narrow width lands on the list. `__cbOpen` is tried up to 40 times, 500 ms apart. Only if no row is found does the app open the URL. It then waits up to 20 seconds for the conversation to appear, and reports an error in the tray if it does not.

Facebook's back button only hides the conversation and keeps it in the DOM, so the path alone cannot tell whether it is showing. `__cbCurrent` also checks `[role=log]` and the `visibility` of `[role=main]`.

The CSS in `panel.js` hides `[role=banner]`, makes `[role=main]` containing `[role=log]` fill the panel, places the list full-size directly beneath it, and changes the scrollbar to 6 px with no arrows.

The panel hides when it loses focus or when you press `Esc`. If at that moment it is showing a different conversation from the bubble you clicked, `BubbleSet.SwitchTo` turns the bubble into that conversation and keeps its position. Exceptions:

| Case | Result |
|---|---|
| The old bubble has newer unread messages | Keeps the old bubble, adds a new one |
| The new conversation already has a bubble | Closes the old bubble, the other stays where it is |
| Opened from the persistent bubble or `+N` | Adds a new bubble |
| Already 5 bubbles, all unread | Adds nothing |
| The conversation is not in the watcher page's list | No change, since there is no name or avatar |

## Bubbles

Up to 5 bubbles show at once. From the sixth on, a new bubble replaces the oldest one with no unread messages. If all 5 are unread, it goes into a `+N` bubble and moves up when a slot frees.

A new bubble goes above the column's stack, then below the anchor when the top is full, then to the next column inward when the column is full. The anchor is where a bubble was last dropped. Until then it is the right edge of the primary screen, a quarter of the way down.

A dropped bubble glides to the nearest edge. Dropping it in the 240×140 px zone at the bottom center closes it, and it comes back on a new message. Clicking a bubble clears its count at once, without waiting for Facebook to report it read, so the count afterwards only includes messages that came after the click.

The preview card sits beside the bubble. If it would cover an open panel, it moves above the panel, or below when there is no room. With no room on either side it still overlaps.

When a screen is plugged in or removed, `SystemEvents.DisplaySettingsChanged` brings off-screen bubbles back to the primary screen.

| Constant | Value | Location |
|---|---|---|
| Bubble size | 56 DIP | `BubbleWindow` |
| Bubble spacing | 8 | `BubbleController.Spacing` |
| Drag threshold | 4 px | `BubbleWindow.DragThreshold` |
| Glide time | 180 ms | `FloatingWindow.GlideMs` |
| Pop and dismiss time | 180 ms | `BubbleWindow.PopTime` |
| Offset between stacked bubbles | 4 | `BubbleController.StackPeek` |
| Regroup time | 5 seconds | `BubbleController._collapse` |

All animation is off when `SystemParameters.ClientAreaAnimation` is `false`, which is when Windows "Animation effects" is off.

## Windows and DPI

The app declares `PerMonitorV2` in `app.manifest`. Floating window positions are read and written in physical pixels through `GetWindowRect` and `SetWindowPos`, so they stay correct across screens with different DPI. The panel size is converted from DIP to pixels using the DPI of the screen holding the bubble, and is limited to the work area.

Floating windows use `WS_EX_NOACTIVATE` so they never take focus and `WS_EX_TOOLWINDOW` so they stay off the taskbar. The panel gets rounded corners from `DwmSetWindowAttribute` with `DWMWA_WINDOW_CORNER_PREFERENCE`.

## Recovery

| Situation | Response |
|---|---|
| The watcher page sends nothing for 60 seconds | Reloads, waiting `5 × 2^n` seconds, 300 seconds at most |
| Navigation fails | Same, unless it was cancelled (`OperationCanceled`) |
| Renderer process dies or hangs | Reloads |
| Browser process dies | Recreates the WebView2 |
| No chat rows for 2 minutes | Red tray icon and an error notice |
| Page moves to `/login`, `/checkpoint` or `/two_step_verification` | Red tray icon and the login window |
| Watcher page ends up on `/` or `/home.php` | Returns to `/messages` |
| Unhandled exception on the UI thread | Logs the exception type and keeps running |

The health check runs every 30 seconds and after each batch of data.

## Data and security

Everything is in `%LOCALAPPDATA%\MessengerBubbles\`:

| Path | Contents |
|---|---|
| `profile\` | WebView2 profile: login session and stored Facebook data, possibly including messages and images. Do not share this folder |
| `settings.json` | Options and bubble positions |
| `app.log` | Status and errors, no names or message text |

`settings.json` is written to a temporary file and then renamed, so it cannot be left half-written. If the file is invalid, the app uses defaults.

| Key | Type | Default |
|---|---|---|
| `PersistentBubble` | bool | `false` |
| `Preview` | bool | `true` |
| `Autostart` | bool | `false` |
| `Sound` | bool | `true` |
| `GroupBubbles` | bool | `false` |
| `CheckUpdates` | bool | `true` |
| `Language` | 0 Vietnamese, 1 English | `null`, meaning not chosen yet |
| `PanelSize` | 0 small, 1 medium, 2 large | `1` |
| `Positions` | Map of `anchor`, `persistent` to `{X, Y, W, H}` | empty |

Restrictions in `AppPaths.Harden` and `FacebookUrl`:

- Denies the page's notification permission.
- Allows navigation only within `https://*.facebook.com` and `about:blank`. Other links the user clicks open in the default browser, for `http` and `https` only.
- Opens no new windows inside the app.
- Rejects conversation links not under `/messages/`.

Apart from the requests the Facebook page makes itself, the app only sends one request to `api.github.com` to ask for the latest version, with no user data in it. GitHub sees the machine's IP address. Turning off `Notify about updates` stops this request. Avatars are read from images the page already loaded, through CDP `Page.getResourceContent`, kept in memory, up to 2 MB each. WebView2 runs with `--disk-cache-size=1`, plus `--disable-background-timer-throttling` and `--disable-renderer-backgrounding` so hidden pages keep running on time. The watcher page is muted.

To uninstall: log out of Facebook through `Open Facebook`, turn off `Start with Windows`, choose `Quit`, then delete the folder above and the `.exe`.

## Reading the log

| Log line | Meaning |
|---|---|
| `new=N` | N new messages detected |
| `health broken=True` | No chat rows could be read for 2 minutes |
| `login needed=True` | Facebook wants you to log in again |
| `panel did not show the thread` | The conversation did not appear after 20 seconds, usually because `panel.js` is out of date |
| `thread row not found, opening by url` | No chat row in the panel, so the URL was opened instead |
| `panel suspend refused` | WebView2 refused to suspend the page |
| `rejected thread link` | Invalid conversation link |
| `bad message from page` | Malformed data from `probe.js` |
| `navigation failed: <status>` | Page load error, will retry |
| `process failed: <kind>` | WebView2 process error |
| `avatar not available: <type>` | Avatar could not be read, the bubble shows an initial |
| `update available: <version>` | A newer version is on GitHub |
| `update check failed: <type>` | Could not reach GitHub, will try again in 24 hours |
| `unhandled: <type>` | Unhandled exception |

## Build

```bash
git clone https://github.com/umefor-labs/MessengerBubbles.git
```

```bash
dotnet run --project src/MessengerBubbles
```

```bash
dotnet test
```

There are 138 tests covering message detection, chat row parsing, the bubble set, geometry, settings, autostart and URLs. The WPF and WebView2 parts have no automated tests and are checked by hand using the [checklist](docs/FACEBOOK-CHECKLIST.md).

Package one self-contained file to `publish\MessengerBubbles.exe`, about 166 MB. The file is uncompressed, because `EnableCompressionInSingleFile` brings it down to 72 MB but costs the app almost 80 MB more private memory:

```bash
dotnet publish src/MessengerBubbles -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Main dependencies: `Microsoft.Web.WebView2` 1.0.2903.40, xUnit 2.5.3. The version for local builds is in `src/MessengerBubbles/MessengerBubbles.csproj`.

## CI and releases

| Workflow | Runs on | Does |
|---|---|---|
| `.github/workflows/test.yml` | Push to `main`, pull request | `dotnet test` |
| `.github/workflows/release.yml` | Push of a `v*` tag | Tests, builds one file, creates a GitHub Release with the `.exe` |

The release version comes from the tag. To create a release:

```bash
git tag -a v0.5.0 -m "v0.5.0"
```

```bash
git push origin v0.5.0
```

## Resources

Measured once on one machine, 3 minutes after starting, with both WebView2 pages loaded:

| Process | Private memory | Working set |
|---|---|---|
| App | 61 MB | 142 MB |
| WebView2, 9 processes | 495 MB | 403 MB |

The working set total counts memory shared between processes more than once. The watcher page runs with `MemoryUsageTargetLevel = Low`.

The build before that setting used 658 MB of WebView2 memory after 6 minutes and 700 MB after 8 hours. The current build has not been measured over a long run, so whether there is a leak is still unknown.

## Known limits

- Two identical messages in a row on a row that stays bold count as one, because the chat list has no per-message signal.
- Muted conversations are recognised only by the shape of the icon, so if Facebook changes it they pop bubbles again. Their messages still count toward the number on the persistent bubble.
- The panel size changes only through the menu, not from the panel itself, because WebView2 covers the resize area unless a border is left around it.
- The panel has no drop shadow. The preview card is always white and does not follow dark mode.
- Dragging a stack into the dismiss zone closes only the top bubble.
- The release file has not been tried on a clean machine.

## Other documents

These are in Vietnamese.

- [Roadmap](docs/ROADMAP.md)
- [Original design](docs/2026-11-02-messenger-bubbles-design.md)
- [Checklist for when Facebook changes its interface](docs/FACEBOOK-CHECKLIST.md)
- [Notes on publishing the repo](docs/PUBLISHING.md)

## License

[MIT](LICENSE).
