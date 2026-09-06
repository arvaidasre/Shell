[![Build](https://github.com/arvaidasre/Shell/actions/workflows/build.yml/badge.svg)](https://github.com/arvaidasre/Shell/actions/workflows/build.yml)

# Shell

Powerful manager for the Windows File Explorer context menu.

> **Maintenance notice:** this repository is now community-maintained by
> [@arvaidasre](https://github.com/arvaidasre). Work on the project continues —
> bug fixes and improvements are in progress. The original project by Nilesoft
> ([moudey/Shell](https://github.com/moudey/Shell), [nilesoft.org](https://nilesoft.org))
> is credited below and in [LICENSE](LICENSE).
>
> **Issues and pull requests are welcome.** If something is broken, please
> [open an issue](https://github.com/arvaidasre/Shell/issues) with your Windows
> version/build, steps to reproduce, and expected behavior.

<p align="center">
  <img src="https://www.nilesoft.org/images/logo-256.png" alt="Shell logo">
</p>

## About

Shell is a context menu extender that lets you handpick the items integrated
into the Windows File Explorer context menu, create custom commands for your
favorite web pages, files and folders, and launch any application directly
from the context menu.

It also lets you modify or remove context menu items added by the system or
third-party software.

## Features

* Lightweight, portable, and relatively easy to use
* Fully customizable appearance
* Custom items: sub-menus, menu items, separators
* Modify or remove existing items
* Supports all file system objects: files, folders, desktop, taskbar
* Expression syntax with built-in functions and predefined variables
* Colors, glyphs, SVG, embedded icons, and image files (.ico, .png, .bmp)
* Search and filter
* Complex nested menus, multiple columns
* Plain-text configuration file
* Minimal resource usage

## Requirements

* Microsoft Windows 7 / 8 / 10 / 11

## Documentation

* [Online documentation (nilesoft.org)](https://nilesoft.org/docs)
* Local docs in [`docs/`](docs)

## Install

Download `Shell-1.9.19-portable-<arch>.zip` (or the `.msi`) from
[Releases](https://github.com/arvaidasre/Shell/releases) — x64 for most
PCs, arm64 for Snapdragon/ARM.

Portable: extract, then in an elevated command prompt in that folder:

    shell -register -treat -restart

(`-treat` replaces the Win11 modern menu; omit it to keep the stock
menu. On Win10 use `shell -register -restart`.)
Unregister with `shell -unregister -restart`.

* Upgrading from the original Nilesoft build: unregister it first.
* New: a per-user config at `%AppData%\Nilesoft\Shell\shell.nss`
  overrides the install-folder file — no admin rights needed, survives
  reinstalls. Full steps in [`docs/installation.html`](docs/installation.html).

The default config is a minimal Win7-style menu (system items only,
no duplicates, type-to-search). Extra menus (Go to, Terminal,
Development, File management) ship in `imports/` — add an
`import 'imports/<name>.nss'` line to `shell.nss` to enable them.

## Building from source

Minimal toolchain (what the CI uses):

* Visual Studio 2022 Build Tools with:
  * `MSVC v143 - VS 2022 C++ x64/x86 build tools`
  * `Windows 11 SDK`
  * `C++ 2022 Redistributable`
  * `NuGet build tools`
* Solution: [`src/Shell.sln`](src/Shell.sln) — `release` / `x64`, `x86`, `arm64`

CI builds every push/PR to `main` — see
[Actions](https://github.com/arvaidasre/Shell/actions).

## Screenshots

<p align="center">
<img src="/screenshots/folder-back.png"><img src="/screenshots/file-manage.png"><br>
<img src="/screenshots/view.png"><img src="/screenshots/edit.png"><br>
<img src="/screenshots/terminal.png"><img src="/screenshots/taskbar.png"><br>
<img src="/screenshots/goto2.png"><img src="/screenshots/gradient.png"><br>
<img src="/screenshots/acrylic.png"><br>
</p>

## Contributing

1. Check [open issues](https://github.com/arvaidasre/Shell/issues) first —
   yours may already be reported.
2. Open a new issue with: Windows version/build, Shell version, repro steps,
   expected vs. actual behavior (screenshots help).
3. Pull requests are welcome — keep changes focused, one issue per PR.

## Credits

Original author: Nilesoft (Mahmoud Gomaa) — [moudey/Shell](https://github.com/moudey/Shell).
This fork continues maintenance with respect for the original work, which
remains licensed under MIT (see [LICENSE](LICENSE)).
