using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using PKHeX.Core;

namespace PkHeXEggLockeGenerator
{
    public class EggLockeGenerator : IPlugin
    {
        public string Name => "EggLocke Generator";
        public int Priority => 1; // Loading order, lowest is first.

        // Initialized on plugin load
        public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
        public IPKMView PKMEditor { get; private set; } = null!;


        private Random random = new Random();

        public void Initialize(params object[] args)
        {
            Console.WriteLine($"Loading {Name}...");
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip)!;
            LoadMenuStrip(menu);
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            if (!(items.Find("Menu_Tools", false)[0] is ToolStripDropDownItem tools))
                throw new ArgumentException(nameof(menuStrip));
            AddPluginControl(tools);
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);

            var c4 = new ToolStripMenuItem("Generate Eggs");
            c4.Click += (s, e) => ModifySaveFile();
            ctrl.DropDownItems.Add(c4);
            Console.WriteLine($"{Name} added menu items.");
        }

        private void ModifySaveFile()
        {
            //! This is ugly but it works for now.
            MsgBoxResult isOK = Interaction.MsgBox("Please note that this will not generate legal Pokémon, and will overwrite the box you select.\n\nDo you wish to continue?",
                MsgBoxStyle.YesNo, "Notice"
            );

            if (isOK != MsgBoxResult.Yes)
            {
                return;
            }

            SaveFile sav = SaveFileEditor.SAV;
            List<PKM> generatedPokemon = new List<PKM>();
            int maxSpecies = sav.MaxSpeciesID;
            GameVersion version = sav.Version;

            // Check if game can even have Eggs
            if (!Breeding.CanGameGenerateEggs(sav.Context.GetSingleGameVersion()))
            {
                //! This is ugly but it works for now.
                Interaction.MsgBox(sav.Context.GetSingleGameVersion().ToString() + " does not support egg generation.", MsgBoxStyle.OkOnly, "Error");
                return;
            }

            // Get slot size of PC boxes
            int PCBoxSize = sav.BoxSlotCount;

            //! This is ugly but it works for now.
            //! TODO - Build an actual GUI for this
            string PCBox = Interaction.InputBox("Please enter the PC Box Number you want to fill.", "Egg Generator", "2");

            int PCBoxID = 1;
            bool converted = Int32.TryParse(PCBox, out PCBoxID);

            // Failcheck for number entry
            if (!converted) {
                //! This is ugly but it works for now.
                Interaction.MsgBox("Failed to process input.", MsgBoxStyle.OkOnly, "Error");
                return;
            }

            // If PC Box doesn't technically exist, throw an error
            if (PCBoxID < 1 || PCBoxID > sav.BoxCount - 1)
            {
                //! This is ugly but it works for now.
                Interaction.MsgBox("PC Box "+PCBoxID+" does not exist.", MsgBoxStyle.OkOnly, "Error");
                return;
            }

            PCBoxID -= 1;

            // Lets generate a pokemon for every box slot!
            for (int i = 0; i < PCBoxSize; i++)
            {
                PKM pkm = EntityBlank.GetBlank(sav.Generation, version);
                pkm.Species = (ushort)random.Next(1, maxSpecies);
                pkm.SetSuggestedFormArgument(pkm.Species);

                generatedPokemon.Add(GenerateEggs(pkm, sav, version));
            }

            // Shuffle them around for an extra bit of randomness
            var shuffled = generatedPokemon.OrderByCustom(x => random.Next()).ToList();

            // Set them in the box
            sav.ImportPKMs(shuffled, true, PCBoxID);
            SaveFileEditor.ReloadSlots();
        }

        public void ModifyPKM(PKM pkm)
        {
            SaveFile sav = SaveFileEditor.SAV;

            pkm = GenerateEggs(pkm, sav, sav.Version);
        }

        public PKM GenerateEggs(PKM pkm, SaveFile sav, GameVersion version)
        {
            // Generate new Egg Pokemon
            EncounterEgg egg = new EncounterEgg(pkm.Species, pkm.Form, EggStateLegality.GetEggLevel(sav.Generation), sav.Generation, version, sav.Context);

            pkm = egg.ConvertToPKM(sav);
            
            // Setup Egg Data
            pkm.Nickname = SpeciesName.GetSpeciesNameGeneration(0, sav.Language, sav.Generation);
            pkm.IsNicknamed = true;
            pkm.OT_Friendship = EggStateLegality.GetMinimumEggHatchCycles(pkm);
            pkm.Met_Location = 0;
            pkm.Gender = pkm.GetSaneGender();

            // Give us a legit Met Location. Not sure if this is really needed but added just in case of game quirks
            /// Credit to https://github.com/CDNRae/pkhex-bulk-egg-generator for how this code works
            /// https://github.com/CDNRae/pkhex-bulk-egg-generator/blob/master/BulkImporter/BulkImporter.cs#L342
            switch (version)
            {
                case GameVersion.BD:
                case GameVersion.SP:
                case GameVersion.BDSP:
                    pkm.Met_Location = 65535;
                    break;
                case GameVersion.Pt:
                    pkm.Egg_Location = 2000;
                    pkm.IsNicknamed = false;
                    pkm.Met_Level = 0;
                    pkm.Version = (int)version;
                    break;
                case GameVersion.D:
                case GameVersion.P:
                case GameVersion.HG:
                case GameVersion.SS:
                case GameVersion.DP:
                case GameVersion.HGSS:
                    pkm.IsNicknamed = false;
                    pkm.Version = (int)sav.Context.GetSingleGameVersion();
                    pkm.Egg_Location = 2000;
                    break;
                case GameVersion.FR:
                case GameVersion.LG:
                case GameVersion.FRLG:
                    pkm.Met_Location = Locations.HatchLocationFRLG;
                    break;
                case GameVersion.R:
                case GameVersion.S:
                case GameVersion.E:
                case GameVersion.RS:
                case GameVersion.RSE:
                    pkm.Met_Location = Locations.HatchLocationRSE;
                    pkm.Version = (int)sav.Context.GetSingleGameVersion();
                    break;
                case GameVersion.GSC:
                    pkm.SetNickname("EGG");
                    break;
                case GameVersion.SV:
                    pkm.Met_Location = Locations.HatchLocation9;
                    if (pkm is ITeraType tera)
                    {
                        var type = Tera9RNG.GetTeraTypeFromPersonal(pkm.Species, pkm.Form, Util.Rand.Rand64());
                        tera.TeraTypeOriginal = (MoveType)type;
                    }
                    break;
            }

            // Randomize IV and EV values
            pkm.IV_HP  = random.Next(0, 31);
            pkm.IV_ATK = random.Next(0, 31);
            pkm.IV_DEF = random.Next(0, 31);
            pkm.IV_SPA = random.Next(0, 31);
            pkm.IV_SPD = random.Next(0, 31);
            pkm.IV_SPE = random.Next(0, 31);

            pkm.EV_HP  = random.Next(0, 255);
            pkm.EV_ATK = random.Next(0, 255);
            pkm.EV_DEF = random.Next(0, 255);
            pkm.EV_SPA = random.Next(0, 255);
            pkm.EV_SPD = random.Next(0, 255);
            pkm.EV_SPE = random.Next(0, 255);

            // Maybe Give Egg Item (Earliest Gen 5 allows this)
            if (sav.Generation >= 5)
            {
                // Get Max ItemID
                int maxItemID = sav.MaxItemID;

                int itemChance = random.Next(0, 100);
                if (itemChance <= 70)
                {
                    PKM item_pkm = pkm;
                    int randItem = random.Next(0, maxItemID);

                    // Check if Item can even be technically held.
                    item_pkm.ApplyHeldItem(randItem, sav.Context);
                    if (ItemRestrictions.IsHeldItemAllowed(item_pkm))
                    {
                        pkm = item_pkm;
                    }
                }
            }

            // Generate Random Ability
            // Pure randomness only works Gen5 and later
            // Gen4 and earlier is a coinflip on hidden ability
            pkm.Ability = random.Next(1, sav.MaxAbilityID);

            // Maybe set Shiny
            int shinyChance = random.Next(1, 100);
            pkm.SetIsShiny(shinyChance <= 2);

            // Setup Move Pool
            int maxMoves = sav.MaxMoveID;
            int moveCount = random.Next(1, 4);

            // Clear auto-generated move pool.
            pkm.Move1 = (ushort)Move.None;
            pkm.Move2 = (ushort)Move.None;
            pkm.Move3 = (ushort)Move.None;
            pkm.Move4 = (ushort)Move.None;
            pkm.FixMoves();

            // Assign new moves randomly
            if (moveCount > 0)
            {
                ushort nextMove = 0;
                int attempt = 0;
                do
                {
                    // Get next Move ID
                    nextMove = (ushort)random.Next(0, maxMoves);

                    // Try to add Move to Move List
                    pkm.PushMove(nextMove);

                    attempt++;
                } while (pkm.MoveCount <= moveCount && attempt < 25);

                // Fix move PP
                pkm.FixMoves();

                if (sav.Generation > 5)
                {
                    // Assign current moves as Relearn moves
                    pkm.SetRelearnMoves(pkm.Moves);
                }
            }

            // Maybe give Pokerus
            if (sav.Generation > 2) {
                int pkrsChance = random.Next(1, 35565);
                if (pkrsChance <= 2)
                {
                    pkm.PKRS_Infected = true;
                }
            }

            // Make EGG
            pkm.IsEgg = true;

            return pkm;
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }
    }
}
