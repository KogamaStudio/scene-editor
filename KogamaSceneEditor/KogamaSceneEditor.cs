using Il2Cpp;
using MelonLoader;
using KogamaModFramework.Commands;
using KogamaMapIO.Commands;
using KogamaModFramework.Security;
using KogamaModFramework.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore;
using KogamaSceneEditor.Commands;


[assembly: MelonInfo(typeof(KogamaSceneEditor.KogamaSceneEditor), "KogamaSceneEditor", "0.1.0-dev", "Amuarte")]
[assembly: MelonGame("Multiverse ApS", "KoGaMa")]

namespace KogamaSceneEditor;

public class KogamaSceneEditor : MelonMod
{
    public static bool Initialized = false;
    public override void OnInitializeMelon()
    {
        AntiBan.Initialize();
        ThemeCrashFix.Initialize();
        CommandManager.Initialize();

        CommandManager.Register(new TestCommand());
        CommandManager.Register(new SceneCommand());
    }
}

