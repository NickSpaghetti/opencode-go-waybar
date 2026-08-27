# opencode-go-waybar

A Waybar module that reports OpenCode Go usage, and an Avalonia window that shows the detail behind it.


![2026-08-26-204502_hyprshot.png](images/2026-08-26-204502_hyprshot.png)

Two binaries come out of this repository:

| Binary | it's is function                                                         |
|---|--------------------------------------------------------------------------|
| `opencode-go-waybar` | The module. One shot, prints JSON to stdout, exits. NativeAOT, linux-x64. |
| `opencode-go-waybar-ui` | The detail window. Avalonia 12.1, native Wayland with an X11 fallback.   |

## What it reads

Usage comes from the OpenCode Go API at `https://opencode.ai/zen/go/v1/usage`. Daily token and cost
totals come from opencode's own SQLite database, which this software only ever reads. Neither is
polled harder than it needs to be: the API answer is cached and refreshed on an interval, and the
database is re-read only when its mtime changes.

The API key is read from `OPENCODE_GO_API_KEY`, from .NET user secrets in a development build, or
from opencode's credential store at `~/.local/share/opencode/auth.json`. The default is to prefer
the configured key and fall back to the credential store, so the module works once you have run
`/connect` in opencode.

## Theme

The window reads your Waybar stylesheet and paints itself in the same colours. It starts at
`~/.config/waybar/style.css`, follows the `@import` chain, and pulls the `@define-color` values out
of whatever that resolves to.

Names are read in two dialects. Catppuccin-style themes say `base`, `text`, `surface0`, `overlay0`.
Palettes generated from a terminal theme say `background` and `foreground`. Both work. A role the
stylesheet does not name is derived from the background and foreground pair, because most
hand-written Waybar themes define a dozen colours and no hairline.

Saving the stylesheet repaints an open window. Light or dark is decided by the luminance of the
background colour.

## Requirements

- .NET 10 SDK
- Docker, for the test and NativeAOT build targets
- Wayland client libraries for the window: `wayland`, `libxkbcommon`, `mesa`
- A Nerd Font, if your bar uses one. The window picks up the family your stylesheet names.

## Build and test

```
make test          # unit suite, in the dev container
make build         # NativeAOT release publish, in Docker
make ui-test       # Avalonia unit tests, on the host
make ui-run        # run the window here (ARGS=--rings|--dashboard|--light)
make integration   # real brokers against the real OS
make acceptance    # drives the published NativeAOT binary in a container
make install       # install the module and window
```

`make help` lists the rest.

```
docker build --platform=linux/amd64 --target final -f Dockerfile -t opencode-go-waybar-prod .
```

Note that `make test` bind-mounts the repository into a Linux container and writes Linux build
output into `bin/`. A later `dotnet run --no-build` on macOS then fails with `Exec format error`.
Rebuild after running the container gate.

## Install

Run `make install` to install the module then modify your Waybar config to use it.
```json
    "custom/opencode-go": {
        "exec": "opencode-go-waybar",
        "exec-if": "pgrep -x opencode >/dev/null",
        "return-type": "json",
        "interval": 5,
        "tooltip": true,
        "format": "{}",
        "on-click": "opencode-go-waybar-ui --dashboard",
        "on-click-right": "opencode-go-waybar-ui --rings",
        "on-click-middle": "opencode-go-waybar-ui --meter"
    }
```


modify your styles.css for waybar
```css
#custom-opencode-go {
    color: #99dcdc;
  padding: 0 8px;
    margin: 0 7.5px;
}
#custom-opencode-go.opencode-go-rate-limited {
    color: #0d0d12;
  background-color: @red;
    border-radius: 3px;
}
#custom-opencode-go.error {
    color: #ffaa71;
}
```

## Configuration

Settings load from `~/.config/opencode-go-waybar/config.json`, then environment variables prefixed
`OPENCODE_GO_`, then .NET user secrets. Nothing has to be set.

| Key | Default | What it does |
|---|---|---|
| `RefreshIntervalSeconds` | `300` | How stale an API answer may get. Clamped to 60-3600. |
| `CautionPercent` | `75` | Where a window stops reading as healthy. |
| `DangerPercent` | `90` | Where a window reads as spent. Must sit above `CautionPercent`. |
| `CacheDirectory` | `~/.cache/opencode-go-waybar` | Where the cache files live. |
| `WaybarStylePath` | `~/.config/waybar/style.css` | The stylesheet to read the palette from. |
| `DatabasePath` | `~/.local/share/opencode/opencode.db` | opencode's database. Read only. |
| `AuthPath` | `~/.local/share/opencode/auth.json` | opencode's credential store. |
| `UsageEndpoint` | the OpenCode Go usage API | Must be an absolute https URI. |
| `ApiKeySource` | `Auto` | `Auto`, `Environment`, or `AuthFile`. |
| `ProcessPresentOverride` | unset | Forces process detection. For containers and tests. |

A bad value fails at startup rather than being ignored. Set `CautionPercent` above `DangerPercent`
and the module exits non-zero with `CautionPercent must be below DangerPercent.` on stderr.

## Layout

The module follows The Standard. Brokers talk to the outside world, foundation services own one
resource each, orchestrations combine them, an aggregation ties them into one contract per exposure
surface, and exposers map that contract to a protocol.

```
Brokers/            Caches Configurations Credentials DateTimes Loggings Processes Storages Themes Usages
Services/
  Foundations/      Configurations OpenCodeAuth OpenCodeDatabase Processes Secrets Themes Usage
                    UsageWindowCache UsageHistoryCache
  Orchestrations/   Credentials UsageWindows UsageHistory
  Aggregations/     Usage
Exposers/           Waybar Usages Themes
Configurations/     UsageComposition, the one place the graph is wired
```


## Tests

- Unit tests mock every dependency. 
- Integration tests drive real brokers against the realfilesystem and process table. 
- Acceptance tests run the published NativeAOT binary inside a container, and deliberately carry no project reference so
they can never test a locally built assembly by accident. 
- The contract tier checks the OpenAPI spec and its fixtures.
