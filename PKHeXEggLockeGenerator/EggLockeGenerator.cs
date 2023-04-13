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
            ctrl.Click += (s, e) => DisplayOptions();
            tools.DropDownItems.Add(ctrl);

            //var c4 = new ToolStripMenuItem("Generate Eggs");
            //c4.Click += (s, e) => ModifySaveFile();
            //ctrl.DropDownItems.Add(c4);
            Console.WriteLine($"{Name} added menu items.");
        }

        private void DisplayOptions()
        {
            SaveFile sav = SaveFileEditor.SAV;

            // Check if game can even have Eggs
            if (!Breeding.CanGameGenerateEggs(sav.Context.GetSingleGameVersion()))
            {
                //! This is ugly but it works for now.
                Interaction.MsgBox(sav.Context.GetSingleGameVersion().ToString() + " does not support egg generation.", MsgBoxStyle.OkOnly, "Error");
                return;
            }

            EggLockeOptions options = new EggLockeOptions(SaveFileEditor, PKMEditor);
            options.Show();
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
