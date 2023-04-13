using Microsoft.VisualBasic;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;
using System.Drawing;
using System.Text.RegularExpressions;

namespace PkHeXEggLockeGenerator
{
    public partial class EggLockeOptions : Form, IPlugin
    {

        private Random random = new Random();

        public int Priority => 1; // Loading order, lowest is first.

        // Initialized on plugin load
        public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
        public IPKMView PKMEditor { get; private set; } = null!;

        private bool doRandomizeEVs = false;
        private bool doRandomizeMoves = false;
        private bool doRandomizeAbilities = false;
        private bool doRandomizeItem = false;

        private int evHPMinValue = 0;
        private int evHPMaxValue = 0;
        private int evATKMinValue = 0;
        private int evATKMaxValue = 0;
        private int evDEFMinValue = 0;
        private int evDEFMaxValue = 0;
        private int evSPAMinValue = 0;
        private int evSPAMaxValue = 0;
        private int evSPDMinValue = 0;
        private int evSPDMaxValue = 0;
        private int evSPEMinValue = 0;
        private int evSPEMaxValue = 0;

        private int shinyChanceValue = 2;
        private int shinyChanceMaxValue = 100;
        private int pokerusChanceValue = 1;
        private int pokerusChanceMaxValue = 65535;
        private int maxMovesValue = 4;

        public void Initialize(params object[] args)
        {
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;
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

        public EggLockeOptions(ISaveFileProvider SaveFileEditor, IPKMView PKMEditor)
        {
            InitializeComponent();

            this.SaveFileEditor = SaveFileEditor;
            this.PKMEditor = PKMEditor;

            selectedBoxes.Items.Clear();

            int boxes = SaveFileEditor.SAV.BoxCount;
            for (int i = 1; i < boxes + 1; i++)
            {
                selectedBoxes.Items.Add($"Box {i}", (i == 2 ? true : false));
            }
        }

        private void enableForm(bool enable)
        {
            foreach (Control c in this.Controls)
            {
                c.Enabled = enable;
            }
            this.Enabled = enable;
            UseWaitCursor = !enable;
        }

        private void clearLog()
        {
            logBox.Clear();
        }

        private void log(string message)
        {
            logBox.AppendText(message + "\n");
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            clearLog();
            enableForm(false);

            int randomSeedValue = (int)randomSeed.Value;
            if (randomSeedValue > -1)
            {
                log($"Using seed {randomSeedValue}.");
                random = new Random(randomSeedValue);
            }
            else
            {
                log("Seed not set. Generating random seed.");
                int newSeed = random.Next();
                random = new Random(newSeed);
                randomSeed.Value = (decimal)newSeed;
                log($"Random seed - {newSeed}.");
            }

            SaveFile sav = SaveFileEditor.SAV;
            int maxSpecies = sav.MaxSpeciesID;
            GameVersion version = sav.Version;

            doRandomizeEVs = randomizeEVs.Checked;
            doRandomizeMoves = randomizeStartingMoves.Checked;
            doRandomizeAbilities = randomizeAbility.Checked;
            doRandomizeItem = randomizeHeldItem.Checked;

            evHPMinValue = (int)evHPMin.Value;
            evHPMaxValue = (int)evHPMax.Value;
            evATKMinValue = (int)evATKMin.Value;
            evATKMaxValue = (int)evATKMax.Value;
            evDEFMinValue = (int)evDEFMin.Value;
            evDEFMaxValue = (int)evDEFMax.Value;
            evSPAMinValue = (int)evSPAMin.Value;
            evSPAMaxValue = (int)evSPAMax.Value;
            evSPDMinValue = (int)evSPDMin.Value;
            evSPDMaxValue = (int)evSPDMax.Value;
            evSPEMinValue = (int)evSPEMin.Value;
            evSPEMaxValue = (int)evSPEMax.Value;

            shinyChanceValue = (int)shinyChance.Value;
            shinyChanceMaxValue = (int)shinyChanceMax.Value;
            pokerusChanceValue = (int)pokerusChance.Value;
            pokerusChanceMaxValue = (int)pokerusChanceMax.Value;

            maxMovesValue = (int)maxMoves.Value;

            log("Checking PC Boxes...");
            List<int> pcBoxes = new List<int>();
            foreach (object itemChecked in selectedBoxes.CheckedItems)
            {
                string? selectedBox = itemChecked as string;
                if (selectedBox != null)
                {
                    string boxIDString = Regex.Replace(selectedBox, "[^0-9]", "");
                    int boxID = 1;
                    bool converted = int.TryParse(boxIDString, out boxID);
                    if (converted)
                    {
                        log($"Adding PC Box {boxID}.");
                        pcBoxes.Add(boxID - 1);
                    }
                    else
                    {
                        Interaction.MsgBox($"Failed to add box id {boxIDString}", MsgBoxStyle.OkOnly, "Error");
                        enableForm(true);
                        return;
                    }
                }
            }

            // Get slot size of PC boxes
            int pcBoxSize = sav.BoxSlotCount;
            log($"PC Box Size : {pcBoxSize}.");

            for (int i = 0; i < pcBoxes.Count; i++)
            {
                List<PKM> generatedPokemon = new List<PKM>();
                int boxID = pcBoxes[i];
                log($"Generating Pokemon for Box ID {boxID + 1}...");
                // Lets generate a pokemon for every box slot!
                for (int j = 0; j < pcBoxSize; j++)
                {
                    log($"Generating Pokemon #{j + 1}...");
                    PKM pkm = EntityBlank.GetBlank(sav.Generation, version);
                    pkm.Species = (ushort)random.Next(1, maxSpecies);
                    pkm.SetSuggestedFormArgument(pkm.Species);

                    generatedPokemon.Add(GenerateEggs(pkm, sav, version));
                }

                log($"Shuffling Pokemon around Box ID {boxID + 1}...");
                // Shuffle them around for an extra bit of randomness
                var shuffled = generatedPokemon.OrderByCustom(x => random.Next()).ToList();

                log($"Adding Pokemon to Box ID {boxID + 1}...");
                // Set them in the box
                sav.ImportPKMs(shuffled, true, boxID);
            }
            SaveFileEditor.ReloadSlots();
            log("Done!");

            Interaction.MsgBox($"Generation Complete.\n\nSeed : {randomSeed.Value}", MsgBoxStyle.OkOnly, "Notice");
            enableForm(true);
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
            pkm.IV_HP = random.Next(0, 31);
            pkm.IV_ATK = random.Next(0, 31);
            pkm.IV_DEF = random.Next(0, 31);
            pkm.IV_SPA = random.Next(0, 31);
            pkm.IV_SPD = random.Next(0, 31);
            pkm.IV_SPE = random.Next(0, 31);

            if (doRandomizeEVs)
            {
                if (evHPMinValue > evHPMaxValue) evHPMaxValue = evHPMinValue;
                if (evATKMinValue > evATKMaxValue) evATKMaxValue = evATKMinValue;
                if (evDEFMinValue > evDEFMaxValue) evDEFMaxValue = evDEFMinValue;
                if (evSPAMinValue > evSPAMaxValue) evSPAMaxValue = evSPAMinValue;
                if (evSPDMinValue > evSPDMaxValue) evSPDMaxValue = evSPDMinValue;
                if (evSPEMinValue > evSPEMaxValue) evSPEMaxValue = evSPEMinValue;

                pkm.EV_HP = random.Next(evHPMinValue, evHPMaxValue);
                pkm.EV_ATK = random.Next(evATKMinValue, evATKMaxValue);
                pkm.EV_DEF = random.Next(evDEFMinValue, evDEFMaxValue);
                pkm.EV_SPA = random.Next(evSPAMinValue, evSPAMaxValue);
                pkm.EV_SPD = random.Next(evSPDMinValue, evSPDMaxValue);
                pkm.EV_SPE = random.Next(evSPEMinValue, evSPEMaxValue);
            }

            // Maybe Give Egg Item (Earliest Gen 5 allows this)
            if (doRandomizeItem)
            {
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
            }

            // Generate Random Ability
            // Pure randomness only works Gen5 and later
            // Gen4 and earlier is a coinflip on hidden ability
            if (doRandomizeAbilities)
            {
                pkm.Ability = random.Next(1, sav.MaxAbilityID);
            }

            // Maybe set Shiny
            if (shinyChanceValue > 0)
            {
                if (shinyChanceValue > shinyChanceMaxValue) shinyChanceMaxValue = shinyChanceValue;
                int chance = random.Next(0, shinyChanceMaxValue);
                pkm.SetIsShiny(shinyChanceValue >= chance);
            }

            // Setup Move Pool
            if (doRandomizeMoves)
            {
                int maxMoves = sav.MaxMoveID;
                int moveCount = random.Next(1, maxMovesValue);

                // Clear auto-generated move pool.
                pkm.Move1 = (ushort)PKHeX.Core.Move.None;
                pkm.Move2 = (ushort)PKHeX.Core.Move.None;
                pkm.Move3 = (ushort)PKHeX.Core.Move.None;
                pkm.Move4 = (ushort)PKHeX.Core.Move.None;
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
                    } while (pkm.MoveCount < moveCount && attempt < 25);

                    // Fix move PP
                    pkm.FixMoves();

                    if (sav.Generation > 5)
                    {
                        // Assign current moves as Relearn moves
                        pkm.SetRelearnMoves(pkm.Moves);
                    }
                }
            }

            // Maybe give Pokerus
            if (pokerusChanceValue > 0)
            {
                if (pokerusChanceValue > pokerusChanceMaxValue) pokerusChanceMaxValue = pokerusChanceValue;
                int pkrsChance = random.Next(0, shinyChanceMaxValue);

                if (pokerusChanceValue <= pkrsChance)
                {
                    pkm.PKRS_Infected = true;
                    pkm.PKRS_Days = 1;
                    pkm.PKRS_Strain = 1;
                    pkm.PKRS_Cured = false;
                }
            }

            // Make EGG
            pkm.IsEgg = true;

            return pkm;
        }

        private void randomizeEVs_CheckedChanged(object sender, EventArgs e)
        {
            randomizeEVOptions.Enabled = randomizeEVs.Checked;
        }

        private void randomizeStartingMoves_CheckedChanged(object sender, EventArgs e)
        {
            randomizeMoveOptions.Enabled = randomizeStartingMoves.Checked;
        }

        private void randomSeed_Leave(object sender, EventArgs e)
        {
            if (randomSeed.Text == "")
            {
                randomSeed.Text = "-1";
            }
        }
    }
}
