# PKHeX Egglocke Generator
![Pokemon Egglocke Generator](https://repository-images.githubusercontent.com/627181349/167135ab-b738-4858-8781-0ed97235573c)

A plugin for [PKHeX](https://github.com/kwsch/PKHeX) that generates a PC Box full of randomly generated Pokemon.

---

## **NOTICE**
**The Pokemon generated with this tool *WILL NOT BE LEGAL***.

If you want a legal egg generator, please see [PkHeX Bulk Egg Generator](https://github.com/CDNRae/pkhex-bulk-egg-generator).

---
## Features
**Fully randomly generated Pokemon Eggs.**

The current list of attributes of a Pokemon that can be random:
- Species
- Gender
- IV Values (1-31)
- EV Values (0-255)
- Shiny-ness chance (default 2%)
- Pokerus chance (default 1 in 65535)
- Starting Moveset, between 1-4 randomly picked moves
- Ability (Coinflip on Hidden or Natural Gen4 and earlier, Purely random Gen5 or later)
- Held Item (Gen5 or later)


---

## How To Use

- Download the latest release of [PKHeX](https://github.com/kwsch/PKHeX) and install/extract it.
- Download and extract the latest release next to PkHeX.exe.
    - Your folder should look like this:
```
PkHeX/
    PkHeX.exe
    plugins/
        PkHeXEgglockeGenerator.dll
```

- Open PkHeX
- If not setup already, click `Options` then `Settings`, and navigate to the `Startup` tab.
    - Set `ForceHaXOnLaunch` to `True`
- From the `Tools` dropdown, hover `Egglocke Generator` and click `Generate Eggs`.

![EggLocke Options Screenshot](/screenshot.png)

---

## Disclaimer
Thanks to **CDNRae** ([PkHeX Bulk Egg Generator](https://github.com/CDNRae/pkhex-bulk-egg-generator)) for their public code allowing me to understand the PkHeX API by referencing and poking some of their code.

I made this plugin purely for fun and there's no guarentee it'll get updates in the future. If you do find an issue, please report it in **Issues** and I'll attempt to fix the problem.