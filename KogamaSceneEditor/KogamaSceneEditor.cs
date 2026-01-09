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
using KogamaModFramework.Data;
using UnityEngine.UIElements;
using UnityEngine;
using KogamaModFramework.Operations;


[assembly: MelonInfo(typeof(KogamaSceneEditor.KogamaSceneEditor), "KogamaSceneEditor", "0.1.0-dev", "Amuarte")]
[assembly: MelonGame("Multiverse ApS", "KoGaMa")]

namespace KogamaSceneEditor;

public class KogamaSceneEditor : MelonMod
{
    public static bool Initialized = false;
    public override void OnInitializeMelon()
    {
    }

    public override void OnUpdate()
    {
        if (MVGameControllerBase.IsInitialized &&  !Initialized)
        {
            AntiBan.Initialize();
            ThemeCrashFix.Initialize();
            CommandManager.Initialize();

            CommandManager.Register(new TestCommand());
            CommandManager.Register(new SceneCommand());

            KogamaModFramework.UI.ContextMenuManager.AddButton("Copy ItemId", wo => true, wo =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo("cmd", $"/c set /p=\"{wo.itemId}\" <nul | clip")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);
                TextCommand.NotifyUser($"Copied: {wo.itemId}");

            });
            Initialized = true;
        }
    }
}

