# PKHeX Egglocke Generator

A plugin for [PKHeX](https://github.com/kwsch/PKHeX) that generates a PC Box full of randomly generated Pokemon.

---

## **NOTICE**
**The Pokemon generated with this tool *WILL NOT BE LEGAL***.

If you want a legal egg generator, please see [PkHeX Bulk Egg Generator](https://github.com/CDNRae/pkhex-bulk-egg-generator).

---
## Features
**Fully randomly generated Pokemon Eggs.**

The current list of attributes of a Pokemon that will be random:
- Species
- Gender
- IV Values (1-31)
- EV Values (1-255)
- Shiny-ness chance (fixed 2%)
- Starting Moveset, between 1-4 randomly picked moves
- Ability (Coinflip on Hidden or Natural Gen4 and earlier, Purely random Gen5 or later)
- Held Item (Gen5 or later)
- Very very small chance of Pokerus. (fixed 1 in 35565 chances)

---

## Features To Implement
- A functioning GUI to allow fine-tuning

---

## How To Use

- Download the latest release of [PKHeX](https://github.com/kwsch/PKHeX) and install/extract it.
- Download and extract the latest release next to PkHeX.exe.
    - Your folder should look like this:
```
PkHeX/
    PkHeX.exe
    plugins/
        PkHeXEgglockGenerator.dll
```

- Open PkHeX
- If not setup already, click `Options` then `Settings`, and navigate to the `Startup` tab.
    - Set `ForceHaXOnLaunch` to `True`
- From the `Tools` dropdown, hover `EgglockGenerator` and click `Generate Eggs`.
- Enter the Box ID you wish to fill with Eggs. (Defaults to `2`)
- Congrats, you have a box full of randomly generated Pokemon! You can repeat the steps to fill as many boxes as you'd like.

---

## Disclaimer
Thanks to **CDNRae** ([PkHeX Bulk Egg Generator](https://github.com/CDNRae/pkhex-bulk-egg-generator)) for their public code allowing me to understand the PkHeX API by referencing and poking some of their code.

I made this plugin purely for fun and there's no guarentee it'll get updates in the future. If you do find an issue, please report it in **Issues** and I'll attempt to fix the problem.