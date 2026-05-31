# Resource Value Calculator

A Windows desktop app for **Star Citizen** that values raw resources and crafting
components using **live UEX market prices** and **datamined crafting blueprints**.

Work out what a haul of refined materials is worth, or how much the resources to
craft a given component (a weapon, armor piece, ship component, …) are worth — all
priced at the best terminal sell price pulled live from [UEX](https://uexcorp.space).

> ⚠️ Unofficial fan-made tool. Not affiliated with Cloud Imperium Games. Star Citizen
> is a trademark of Cloud Imperium Rights LLC.

---

## Features

The app has three tabs:

### 🧮 Resource Value Calculator
- Add rows of resources; pick each from a searchable dropdown of UEX commodities.
- **Base Price auto-fills** with the commodity's **highest terminal sell price** (per SCU).
- Enter **SCU Qty** and **Base Quality**; tweak a global **Profit Margin** multiplier.
- Each row's price and the **grand total** update as you edit.

### 🛠️ Components
- Pick a crafting **blueprint** — filter by category (Vehicle Weapon, FPS Weapon, Armor,
  Cooler, Power Plant, Shield, Quantum Drive, …) and type-ahead search by name.
- See the **fixed bill of materials** for that component, each resource priced from UEX.
- **Base Quality** is editable per material; quantities come from the blueprint.
- Shows the **total resource value** required to craft the item.

### 🔄 Update Data
- Imports the latest crafting blueprints directly from
  [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) on GitHub.
- Distills ~1,500+ blueprints into a local catalog (saved to `%LocalAppData%`), so it
  works after install without elevation and persists across launches.

On first run, only **Update Data** is shown — import once to unlock the Calculator and
Components tabs. (A blueprint catalog is also bundled with the app as a fallback.)

---

## How values are calculated

```
Price = (Base Price × SCU Qty) × Base Quality × Profit Margin
```

- **Base Price** — for a given commodity, the highest sell price recorded across all
  terminals (UEX field `price_sell_max`), i.e. the best place to sell.
- **Profit Margin** — a global multiplier (default `1.2`) applied to every row.
- The **grand total** sums every row's price.

---

## Getting started

### Requirements
- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build, or the
  **.NET 10 Desktop Runtime** to run the published/installed app.

### Run from source
```sh
dotnet run --project ResourceValueCalculator/ResourceValueCalculator.csproj
```

### Build
```sh
dotnet build ResourceValueCalculator.slnx -c Release
```

### Build the installer (MSI)
Requires the [WiX Toolset](https://wixtoolset.org) v6 (`dotnet tool install --global wix`).
```sh
dotnet build Installer/Installer.wixproj -c Release
# → Installer/bin/Release/ResourceValueCalculator-Setup.msi
```
The MSI installs to *Program Files*, adds a Start Menu shortcut, and supports
upgrade/uninstall.

---

## Data sources

| Data | Source |
|------|--------|
| Commodity prices (per-terminal sell prices) | [UEX Corp API](https://uexcorp.space/api/documentation) |
| Crafting blueprints (recipes & materials) | [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) (datamined SC game files) |

Prices are fetched live on launch; blueprints are bundled and can be refreshed from the
Update Data tab.

---

## Project structure

```
ResourceValueCalculator.slnx              Solution
ResourceValueCalculator/                  WPF app (.NET 10, MVVM)
  ShellViewModel.cs                        Hosts the tabs, loads shared data
  CalculatorViewModel.cs                   Resource Value Calculator tab
  ComponentViewModel.cs                    Components tab
  ImportViewModel.cs                       Update Data tab
  CommodityService.cs                      UEX price fetching
  BlueprintService.cs / ScunpackedImporter.cs   Blueprint loading & import
  blueprints.json                          Bundled blueprint catalog
Installer/                                 WiX v6 MSI installer
```

## Tech stack
- .NET 10 / WPF (`net10.0-windows`)
- MVVM with `System.Text.Json` and `HttpClient`
- WiX Toolset v6 for the MSI installer

## Acknowledgements
- [UEX Corp](https://uexcorp.space) — community trade data & API
- [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) by StarCitizenWiki — datamined game data
