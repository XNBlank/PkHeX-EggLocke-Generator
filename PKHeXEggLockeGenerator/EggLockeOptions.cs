using Microsoft.VisualBasic;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
        private int minMovesValue = 1;
        private int maxMovesValue = 4;

        private Dictionary<int, string?> blacklistedPokemon = new Dictionary<int, string?>();
        private Dictionary<int, string?> blacklistedAbilites = new Dictionary<int, string?>();
        private Dictionary<int, string?> blacklistedItems = new Dictionary<int, string?>();

        private Dictionary<int, string?> usablePokemon = new Dictionary<int, string?>();
        private Dictionary<int, string?> usableAbilites = new Dictionary<int, string?>();
        private Dictionary<int, string?> usableItems = new Dictionary<int, string?>();

        IEnumerable<int> usablePokemonIDs = Enumerable.Empty<int>();
        IEnumerable<int> usableItemIDs = Enumerable.Empty<int>();
        IEnumerable<int> usableAbilityIDs = Enumerable.Empty<int>();

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
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            InitializeComponent();

            this.SaveFileEditor = SaveFileEditor;
            this.PKMEditor = PKMEditor;

            SaveFile sav = SaveFileEditor.SAV;

            IReadOnlyList<PKM> gamePokemon = sav.GetAllPKM();
            ReadOnlySpan<ushort> heldItems = sav.HeldItems;

            LanguageID lang = Language.GetSafeLanguage(sav.Generation, LanguageID.English, sav.Version);
            GameStrings strings = GameInfo.GetStrings(lang.GetLanguage2CharName());

            // Enable Fairy Type Blacklist if Gen 6 or later
            pokemonBlackListFairy.Enabled = (sav.Generation >= 6);

            selectedBoxes.Items.Clear();

            int boxes = sav.BoxCount;
            for (int i = 1; i < boxes + 1; i++)
            {
                selectedBoxes.Items.Add($"Box {i}", (i == 2 ? true : false));
            }

            pokemonBlackList.Items.Clear();
            Dictionary<int, string> pokemon = Enum.GetValues(typeof(Species)).Cast<Species>().ToDictionary(t => (int)t, t => t.ToString());
            foreach (var i in pokemon)
            {
                string name = SpeciesName.GetSpeciesName((ushort)i.Key, (int)lang);

                if (i.Key == (int)Species.MAX_COUNT) continue;
                if (i.Key == 0) continue; // Skip Egg/None
                if (sav.Personal.IsSpeciesInGame((ushort)i.Key))
                {
                    pokemonBlackList.Items.Add(new Pokemon { DisplayValue = name, Index = i.Key });
                }
            }
            pokemonBlackList.DisplayMember = "DisplayValue";
            pokemonBlackList.ValueMember = "Index";

            pokemonBlackListAbilities.Items.Clear();
            Dictionary<int, string> abilites = Enum.GetValues(typeof(Ability)).Cast<Ability>().ToDictionary(t => (int)t, t => t.ToString());
            foreach (var i in abilites)
            {
                if (i.Key > sav.MaxAbilityID) continue;
                if (i.Key == 0) continue;
                pokemonBlackListAbilities.Items.Add(new PokemonAbility { DisplayValue = i.Value, Index = i.Key });
            }

            pokemonBlackListAbilities.DisplayMember = "DisplayValue";
            pokemonBlackListAbilities.ValueMember = "Index";

            pokemonBlackListItems.Items.Clear();
            foreach (ushort i in heldItems)
            {
                string itemName = strings.itemlist[i];
                if (itemName == "???") continue;
                pokemonBlackListItems.Items.Add(new PokemonHeldItem { DisplayValue = itemName, Index = i });
            }
            pokemonBlackListItems.DisplayMember = "DisplayValue";
            pokemonBlackListItems.ValueMember = "Index";

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

            usablePokemon.Clear();
            usableItems.Clear();
            usableAbilites.Clear();

            blacklistedPokemon.Clear();
            blacklistedItems.Clear();
            blacklistedAbilites.Clear();

            usablePokemonIDs = Enumerable.Empty<int>();
            usableItemIDs = Enumerable.Empty<int>();
            usableAbilityIDs = Enumerable.Empty<int>();

            usablePokemon = pokemonBlackList.Items.Cast<Pokemon>().GroupBy(t => t.Index + ":" + t.DisplayValue, StringComparer.OrdinalIgnoreCase).ToDictionary(t => int.Parse(t.Key.Split(":")[0]), t => t.Key.Split(":")[1]);
            usableItems = pokemonBlackListItems.Items.Cast<PokemonHeldItem>().GroupBy(t => t.Index + ":" + t.DisplayValue, StringComparer.OrdinalIgnoreCase).ToDictionary(t => int.Parse(t.Key.Split(":")[0]), t => t.Key.Split(":")[1]);
            usableAbilites = pokemonBlackListAbilities.Items.Cast<PokemonAbility>().GroupBy(t => t.Index + ":" + t.DisplayValue, StringComparer.OrdinalIgnoreCase).ToDictionary(t => int.Parse(t.Key.Split(":")[0]), t => t.Key.Split(":")[1]);

            blacklistedPokemon = pokemonBlackList.CheckedItems.Cast<Pokemon>().GroupBy(t => t.Index + ":" + t.DisplayValue, StringComparer.OrdinalIgnoreCase).ToDictionary(t => int.Parse(t.Key.Split(":")[0]), t => t.Key.Split(":")[1]);
            blacklistedItems = pokemonBlackListItems.CheckedItems.Cast<PokemonHeldItem>().GroupBy(t => t.Index + ":" + t.DisplayValue, StringComparer.OrdinalIgnoreCase).ToDictionary(t => int.Parse(t.Key.Split(":")[0]), t => t.Key.Split(":")[1]);
            blacklistedAbilites = pokemonBlackListAbilities.CheckedItems.Cast<PokemonAbility>().GroupBy(t => t.Index + ":" + t.DisplayValue, StringComparer.OrdinalIgnoreCase).ToDictionary(t => int.Parse(t.Key.Split(":")[0]), t => t.Key.Split(":")[1]);

            usablePokemonIDs = usablePokemon.Keys.Except(blacklistedPokemon.Keys);
            usableItemIDs = usableItems.Keys.Except(blacklistedItems.Keys);
            usableAbilityIDs = usableAbilites.Keys.Except(blacklistedAbilites.Keys);

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

            minMovesValue = (int)minMoves.Value;
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
                    //pkm.Species = (ushort)random.Next(1, maxSpecies);
                    pkm.Species = (ushort)usablePokemonIDs.ElementAt(random.Next(0, usablePokemonIDs.Count()));
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
            // pkm.OriginalTrainerFriendship = EggStateLegality.GetMinimumEggHatchCycles(pkm);
            pkm.OriginalTrainerFriendship = 1;
            pkm.MetLocation = 0;
            pkm.Gender = pkm.GetSaneGender();

            // Give us a legit Met Location. Not sure if this is really needed but added just in case of game quirks
            /// Credit to https://github.com/CDNRae/pkhex-bulk-egg-generator for how this code works
            /// https://github.com/CDNRae/pkhex-bulk-egg-generator/blob/master/BulkImporter/BulkImporter.cs#L342
            switch (version)
            {
                case GameVersion.BD:
                case GameVersion.SP:
                case GameVersion.BDSP:
                    pkm.MetLocation = 65535;
                    break;
                case GameVersion.Pt:
                    pkm.EggLocation = 2000;
                    pkm.IsNicknamed = false;
                    pkm.MetLevel = 0;
                    pkm.Version = version;
                    break;
                case GameVersion.D:
                case GameVersion.P:
                case GameVersion.HG:
                case GameVersion.SS:
                case GameVersion.DP:
                case GameVersion.HGSS:
                    pkm.IsNicknamed = false;
                    pkm.Version = sav.Context.GetSingleGameVersion();
                    pkm.EggLocation = 2000;
                    break;
                case GameVersion.FR:
                case GameVersion.LG:
                case GameVersion.FRLG:
                    pkm.MetLocation = Locations.HatchLocationFRLG;
                    break;
                case GameVersion.R:
                case GameVersion.S:
                case GameVersion.E:
                case GameVersion.RS:
                case GameVersion.RSE:
                    pkm.MetLocation = Locations.HatchLocationRSE;
                    pkm.Version = sav.Context.GetSingleGameVersion();
                    break;
                case GameVersion.GSC:
                    pkm.SetNickname("EGG");
                    break;
                case GameVersion.SV:
                    pkm.MetLocation = Locations.HatchLocation9;
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
                    //int maxItemID = sav.MaxItemID;

                    int itemChance = random.Next(0, 100);
                    if (itemChance <= 70)
                    {
                        PKM item_pkm = pkm;
                        //int randItem = random.Next(0, maxItemID);
                        int randItem = (ushort)usableItemIDs.ElementAt(random.Next(0, usableItemIDs.Count()));

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
                if (sav.Generation >= 5)
                {
                    pkm.Ability = (ushort)usableAbilityIDs.ElementAt(random.Next(0, usableAbilityIDs.Count()));
                    log($"Setting ability {pkm.Ability}.");
                }
                else if (sav.Generation > 2)
                {
                    pkm.Ability = pkm.PersonalInfo.GetAbilityAtIndex(random.Next(0, 1));
                }
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
                int moveCount = random.Next(minMovesValue, maxMovesValue);

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
                        pkm.AddMove(nextMove);

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
                int pkrsChance = random.Next(0, pokerusChanceMaxValue);

                if (pokerusChanceValue >= pkrsChance)
                {
                    pkm.IsPokerusInfected = true;
                    pkm.PokerusDays = random.Next(1, 3);
                    pkm.PokerusStrain = random.Next(1, 3);
                    pkm.IsPokerusCured = false;
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

        private void pokemonBlackListLegendaries_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pokemonBlackList.Items.Count; ++i)
            {
                Pokemon? pokemon = pokemonBlackList.Items[i] as Pokemon;
                bool blacklist = false;

                if (pokemon != null)
                {
                    switch (pokemon.Index)
                    {
                        case ((int)Species.Shedinja):
                        case ((int)Species.Mew):
                        case ((int)Species.Mewtwo):
                        case ((int)Species.Articuno):
                        case ((int)Species.Moltres):
                        case ((int)Species.Zapdos):
                        case ((int)Species.Celebi):
                        case ((int)Species.Raikou):
                        case ((int)Species.Entei):
                        case ((int)Species.Suicune):
                        case ((int)Species.Lugia):
                        case ((int)Species.HoOh):
                        case ((int)Species.Regirock):
                        case ((int)Species.Regice):
                        case ((int)Species.Registeel):
                        case ((int)Species.Latias):
                        case ((int)Species.Latios):
                        case ((int)Species.Groudon):
                        case ((int)Species.Kyogre):
                        case ((int)Species.Rayquaza):
                        case ((int)Species.Jirachi):
                        case ((int)Species.Deoxys):
                        case ((int)Species.Uxie):
                        case ((int)Species.Mesprit):
                        case ((int)Species.Azelf):
                        case ((int)Species.Heatran):
                        case ((int)Species.Regigigas):
                        case ((int)Species.Cresselia):
                        case ((int)Species.Darkrai):
                        case ((int)Species.Shaymin):
                        case ((int)Species.Manaphy):
                        case ((int)Species.Phione):
                        case ((int)Species.Dialga):
                        case ((int)Species.Palkia):
                        case ((int)Species.Giratina):
                        case ((int)Species.Arceus):
                        case ((int)Species.Victini):
                        case ((int)Species.Cobalion):
                        case ((int)Species.Terrakion):
                        case ((int)Species.Virizion):
                        case ((int)Species.Tornadus):
                        case ((int)Species.Thundurus):
                        case ((int)Species.Landorus):
                        case ((int)Species.Reshiram):
                        case ((int)Species.Zekrom):
                        case ((int)Species.Kyurem):
                        case ((int)Species.Keldeo):
                        case ((int)Species.Meloetta):
                        case ((int)Species.Genesect):
                        case ((int)Species.Diancie):
                        case ((int)Species.Hoopa):
                        case ((int)Species.Volcanion):
                        case ((int)Species.Magearna):
                        case ((int)Species.Xerneas):
                        case ((int)Species.Yveltal):
                        case ((int)Species.Zygarde):
                        case ((int)Species.TypeNull):
                        case ((int)Species.Silvally):
                        case ((int)Species.TapuBulu):
                        case ((int)Species.TapuFini):
                        case ((int)Species.TapuKoko):
                        case ((int)Species.TapuLele):
                        case ((int)Species.Nihilego):
                        case ((int)Species.Buzzwole):
                        case ((int)Species.Pheromosa):
                        case ((int)Species.Xurkitree):
                        case ((int)Species.Celesteela):
                        case ((int)Species.Kartana):
                        case ((int)Species.Guzzlord):
                        case ((int)Species.Poipole):
                        case ((int)Species.Naganadel):
                        case ((int)Species.Stakataka):
                        case ((int)Species.Blacephalon):
                        case ((int)Species.Marshadow):
                        case ((int)Species.Zeraora):
                        case ((int)Species.Meltan):
                        case ((int)Species.Melmetal):
                        case ((int)Species.Cosmog):
                        case ((int)Species.Cosmoem):
                        case ((int)Species.Solgaleo):
                        case ((int)Species.Lunala):
                        case ((int)Species.Necrozma):
                        case ((int)Species.Kubfu):
                        case ((int)Species.Urshifu):
                        case ((int)Species.Regieleki):
                        case ((int)Species.Regidrago):
                        case ((int)Species.Glastrier):
                        case ((int)Species.Spectrier):
                        case ((int)Species.Enamorus):
                        case ((int)Species.Zacian):
                        case ((int)Species.Zamazenta):
                        case ((int)Species.Eternatus):
                        case ((int)Species.Calyrex):
                        case ((int)Species.Zarude):
                        case ((int)Species.TingLu):
                        case ((int)Species.ChienPao):
                        case ((int)Species.WoChien):
                        case ((int)Species.ChiYu):
                        case ((int)Species.Koraidon):
                        case ((int)Species.Miraidon):
                            blacklist = true;
                            break;
                    }

                    if (blacklist)
                    {
                        pokemonBlackList.SetItemChecked(i, blacklist);
                    }
                }
            }
        }

        private void pokemonBlackListDefaultAbilities_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pokemonBlackListAbilities.Items.Count; ++i)
            {
                PokemonAbility? ability = pokemonBlackListAbilities.Items[i] as PokemonAbility;
                bool blacklist = false;

                if (ability != null)
                {
                    switch (ability.DisplayValue)
                    {
                        case "WonderGuard":
                        case "GoodasGold":
                        case "BeastBoost":
                            blacklist = true;
                            break;
                    }

                    if (blacklist) { 
                        log($"Blacklisting {ability.Index} - {ability.DisplayValue}");
                        pokemonBlackListAbilities.SetItemChecked(i, blacklist);
                    }
                }
            }
        }

        private void pokemonBlackListDefaultItems_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pokemonBlackListItems.Items.Count; ++i)
            {
                PokemonHeldItem? item = pokemonBlackListItems.Items[i] as PokemonHeldItem;
                bool blacklist = false;

                if (item != null)
                {
                    switch (item.DisplayValue)
                    {
                        case "Eviolite":
                        case "Assault Vest":
                        case "Weakness Policy":
                        case "Heavy-Duty Boots":
                        case "Leftovers":
                        case "Choice Scarf":
                        case "Choice Band":
                        case "Choice Specs":
                        case "Rocky Helmet":
                        case "Air Balloon":
                        case "Focus Sash":
                        case "Life Orb":
                        case "Booster Energy":
                        case "Punching Glove":
                        case "Loaded Dice":
                            blacklist = true;
                            break;
                    }
                    if (blacklist)
                    {
                        log($"Blacklisting {item.Index} - {item.DisplayValue}");
                        pokemonBlackListItems.SetItemChecked(i, blacklist);
                    }
                }
            }
        }

        private void pokemonBlackListAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pokemonBlackList.Items.Count; ++i)
            {
                pokemonBlackList.SetItemChecked(i, true);
            }
        }

        private void pokemonBlackListNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pokemonBlackList.Items.Count; ++i)
            {
                pokemonBlackList.SetItemChecked(i, false);
            }
        }

        private bool pokemonIsType(PKM pkm, MoveType type)
        {
            if (pkm.PersonalInfo.Type1 == (byte)type || pkm.PersonalInfo.Type2 == (byte)type)
            {
                return true;
            }

            return false;
        }

        private void setBlackListByType(MoveType type)
        {
            for (int i = 0; i < pokemonBlackList.Items.Count; ++i)
            {
                PKM pkm = SaveFileEditor.SAV.BlankPKM;

                Pokemon? pokemonCheckbox = pokemonBlackList.Items[i] as Pokemon;

                if (pokemonCheckbox != null)
                {
                    pkm.Species = (ushort)pokemonCheckbox.Index;

                    bool doCheck = pokemonIsType(pkm, type);

                    if (doCheck) {
                        pokemonBlackList.SetItemChecked(i, true);
                    }
                }
            }
        }

        private void pokemonBlackListNormal_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Normal);
        }

        private void pokemonBlackListFire_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Fire);
        }

        private void pokemonBlackListWater_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Water);
        }

        private void pokemonBlackListGrass_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Grass);
        }

        private void pokemonBlackListFlying_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Flying);
        }

        private void pokemonBlackListElectric_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Electric);
        }

        private void pokemonBlackListRock_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Rock);
        }

        private void pokemonBlackListBug_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Bug);
        }

        private void pokemonBlackListGround_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Ground);
        }

        private void pokemonBlackListPoison_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Poison);
        }

        private void pokemonBlackListIce_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Ice);
        }

        private void pokemonBlackListSteel_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Steel);
        }

        private void pokemonBlackListPsychic_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Psychic);
        }

        private void pokemonBlackListGhost_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Ghost);
        }

        private void pokemonBlackListDark_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Dark);
        }

        private void pokemonBlackListFighting_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Fighting);
        }

        private void pokemonBlackListDragon_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Dragon);
        }

        private void pokemonBlackListFairy_Click(object sender, EventArgs e)
        {
            setBlackListByType(MoveType.Fairy);
        }

    }

    public class Pokemon
    {
        public string? DisplayValue { get; set; }
        public int Index { get; set; }
    }

    public class PokemonHeldItem
    {
        public string? DisplayValue { get; set; }
        public int Index { get; set; }
    }

    public class PokemonAbility
    {
        public string? DisplayValue { get; set; }
        public int Index { get; set; }
    }
}
