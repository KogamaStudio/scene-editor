using Il2Cpp;
using JetBrains.Annotations;
using KogamaModFramework.Commands;
using KogamaModFramework.Operations;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using static Il2CppAssets.Scripts.WorldObjectTypes.Avatar.Accessories.AccessoryLoader;

namespace KogamaMapIO.Commands;

internal class TestCommand : Command
{
    public override string Name => "testt";

    public override void Execute(string[] args)
    {
        int id = int.Parse(args[0]);
        MelonCoroutines.Start(WorldObjectOperations.CloneObjectWithPosition(id, Vector2.zero, Quaternion.identity, (id) =>
        {
            MelonLogger.Msg($"Cloned {id}!");
        }));
    }
}
