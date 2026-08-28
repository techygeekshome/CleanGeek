<div align="center">

<img src="icons/cleangeek.png" alt="CleanGeek logo" width="96" height="96">

# CleanGeek

**Get your disk space back. Without a registry cleaner, and without being sold to.**

[![Status](https://img.shields.io/badge/status-in%20development-b7791f)](https://github.com/techygeekshome/CleanGeek)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078d4)](#getting-it-running)
[![License](https://img.shields.io/badge/License-GPL--3.0-blue)](LICENSE)
[![Made by TechyGeeksHome](https://img.shields.io/badge/made%20by-TechyGeeksHome-b191f2)](https://techygeekshome.info)
[![Support on Ko-fi](https://img.shields.io/badge/support-Ko--fi-ff5e5b)](https://ko-fi.com/techygeekshome)

[What it does](#what-it-does) · [What it refuses to do](#what-it-refuses-to-do) · [Getting it running](#getting-it-running) · [Build from source](#build-from-source) · [Licence](#licence)

</div>

---

There are gigabytes on a typical Windows machine that nothing needs: Windows Update leftovers,
the delivery-optimisation cache, crash dumps, thumbnail and icon caches, old component-store
versions, per-browser caches. CleanGeek finds them, tells you exactly how much each one is
worth, and removes only what you tick.

The scan and the clean are always two separate steps. You see the number before anything is
deleted, and the recovered total afterwards.

## What it refuses to do

This is the other category with a deservedly poor reputation, so again, plainly:

- **There is no registry cleaner, and there will not be one.** It is the headline feature of
  every product in this space and it does not work. Registry cleaning has no measurable benefit
  on any Windows version this decade, Microsoft does not endorse the practice, and the failure
  mode is a machine that will not boot. The upside is zero. We are not shipping it for the sake
  of a bullet point.
- **Saved passwords are not a cleanup target.** Not off by default — not present at all.
- **Cookies, history and saved form data are off by default**, and each one says in plain words
  what clearing it will cost you before you tick it.
- **Nothing is deleted outside a known, named list.** No wildcard sweeps of your profile.
- **The Recycle Bin is only emptied when you explicitly tick it**, never as part of "clean
  everything".
- **No telemetry, no account, no bundled offers, no paid tier.**

## What it does

- 🧹 **Cleans what is actually safe to clean** — temp files, Windows Update and delivery
  optimisation leftovers, crash dumps, thumbnail and icon caches, browser caches, the component
  store, previous Windows installations.
- 🔢 **Shows the number first** — every category reports what it would recover before anything is
  removed.
- ✅ **Nothing ticked by default except caches and temp files.** The riskier categories are opt-in
  every time.
- 🚀 **Startup manager** — see what launches with Windows, and what each entry costs you.
- ⏰ **Scheduled cleaning** — the feature the competition charges for, on the same Task Scheduler
  plumbing AppGeek already uses.
- 🔒 **Skips anything in use** — nothing is removed from under a running process.

### What it deliberately does not duplicate

CleanGeek stays in its lane. The rest of the range already covers the neighbouring jobs:

| If you want to | Use |
|---|---|
| See what is eating your disk, visually | [DiskGeek](https://github.com/techygeekshome/DiskGeek) |
| Update your installed software | [AppGeek](https://github.com/techygeekshome/AppGeek) |
| Update your drivers | [DriverGeek](https://github.com/techygeekshome/DriverGeek) |

## Getting it running

CleanGeek has not had a public release yet, so there is nothing to download at the moment. When
there is, this section will carry the installer and the portable build, both with published
SHA-256 hashes.

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet build src/CleanGeek/CleanGeek.csproj -c Release
```

```powershell
dotnet publish src/CleanGeek/CleanGeek.csproj -c Release -r win-x64 --self-contained true
```

## Support & contributing

Found a bug or have a request? [Open an issue](https://github.com/techygeekshome/CleanGeek/issues)
or [get in touch](https://techygeekshome.info/contact/). Contributions are welcome — see
[CONTRIBUTING.md](CONTRIBUTING.md).

## ☕ Support

If CleanGeek gets you your weekend back, [buy us a coffee](https://ko-fi.com/techygeekshome). It
is never required and nothing is withheld without it.

## Licence

CleanGeek is free software under the **GNU General Public License v3.0** — see [LICENSE](LICENSE)
and [gnu.org](https://www.gnu.org/licenses/gpl-3.0.en.html). Anyone may use, modify and share it;
a distributed modification must publish its source under the same licence.

Free for everyone, including commercial use.

© 2026 TechyGeeksHome | Andrew Armstrong.

---

<div align="center">

Made with ❤️ by [**TechyGeeksHome**](https://techygeekshome.info)

[Website](https://techygeekshome.info) · [YouTube](https://www.youtube.com/channel/UCtEuFj1SMLiuRoucD1hv8dA) · [X](https://x.com/TechyGeeks1) · [Facebook](https://www.facebook.com/techygeeks.home) · [Instagram](https://www.instagram.com/andrewarmstrongtgh)

</div>
