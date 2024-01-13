using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ModLoader;
using UITools;
using UnityEngine;
using HarmonyLib;
using ModLoader.Helpers;
using SFS.IO;

namespace DeltaV_Calculator
{
    public class Main : Mod, IUpdatable
    {
        const string C_STR_MOD_ID = "DELTA_V_CALCULATOR";
        const string C_STR_MOD_NAME = "ΔV calculator";
        const string C_STR_AUTHOR = "Altaïr";
        const string C_STR_MODLOADER_VERSION = "1.5.10.2";
        const string C_STR_MOD_VERSION = "V1.1.3";
        const string C_STR_MOD_DESCRIPTION = "This mod adds your current ΔV to the flight information interface.";

        public override string ModNameID => C_STR_MOD_ID;

        public override string DisplayName => C_STR_MOD_NAME;

        public override string Author => C_STR_AUTHOR;

        public override string MinimumGameVersionNecessary => C_STR_MODLOADER_VERSION;

        public override string ModVersion => C_STR_MOD_VERSION;

        public override string Description => C_STR_MOD_DESCRIPTION;

        public override string IconLink => "https://i.imgur.com/1jDbVB8.png"; // link to the logo

        // Set the dependencies
        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.0" } };

        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath> { { "https://github.com/Kaskouy/SFS-DeltaV-calculator/releases/latest/download/DeltaV_Calculator.dll", new FolderPath(ModFolder).ExtendToFile("DeltaV_Calculator.dll") } };
        
        public Main() : base()
        {
            //Harmony.DEBUG = false;
            //FileLog.logPath = "C:\\Users\\JB\\Desktop\\Jeux\\SFS PC\\DeltaV calculator\\Logs_DeltaV_Calculator.txt";
        }

        // This initializes the patcher. This is required if you use any Harmony patches
        public static Harmony patcher;

        public override void Load()
        {
            // Tells the loader what to run when your mod is loaded
            ModLoader.Helpers.SceneHelper.OnWorldSceneLoaded += new Action(DeltaV_UI.createUI);
        }

        public override void Early_Load()
        {
            // This method run s before anything from the game is loaded. This is where you should apply your patches, as shown below.

            // The patcher uses an ID formatted like a web domain
            Main.patcher = new Harmony($"{C_STR_MOD_ID}.{C_STR_MOD_NAME}.{C_STR_AUTHOR}");

            // This pulls your Harmony patches from everywhere in the namespace and applies them.
            Main.patcher.PatchAll();
            
            //base.early_load();
        }
    }
}
