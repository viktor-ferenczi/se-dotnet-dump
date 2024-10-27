using System;
using System.IO;
using System.Reflection;
using ClientPlugin.Patches;
using HarmonyLib;
using VRage.FileSystem;
using VRage.Plugins;

namespace ClientPlugin
{
    // ReSharper disable once UnusedType.Global
    public class Plugin : IPlugin, IDisposable
    {
        public const string Name = "DotNetDump";
        public static Plugin Instance { get; private set; }
        public static int Ticks { get; private set; }
        public static string DataDir => Path.Combine(MyFileSystem.UserDataPath, Name);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            Instance = this;

            // TODO: Put your one time initialization code here.
            Harmony harmony = new Harmony(Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Directory.CreateDirectory(DataDir);

            DumpReplicationTables.Init();
        }

        public void Dispose()
        {
            // TODO: Save state and close resources here, called when the game exits (not guaranteed!)
            // IMPORTANT: Do NOT call harmony.UnpatchAll() here! It may break other plugins.

            Instance = null;
        }

        public void Update()
        {
            DumpReplicationTables.Update(Ticks);
            
            Ticks++;
        }

        // TODO: Uncomment and use this method to create a plugin configuration dialog
        // ReSharper disable once UnusedMember.Global
        /*public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new MyPluginConfigDialog());
        }*/

        //TODO: Uncomment and use this method to load asset files
        /*public void LoadAssets(string folder)
        {

        }*/
    }
}