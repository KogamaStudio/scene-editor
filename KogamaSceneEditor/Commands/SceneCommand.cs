using KogamaModFramework.Commands;
using KogamaModFramework.Operations;
using KogamaModFramework.Data;
using MelonLoader;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2Cpp;

namespace KogamaSceneEditor.Commands;

internal class SceneCommand : Command
{
    public override string Name => "scene";

    public override void Execute(string[] args)
    {
        if (args.Length < 2)
        {
            TextCommand.NotifyUser("Usage: /scene <jsonfile> <modelId>");
            return;
        }

        int itemId = int.Parse(args[1]);
        MelonCoroutines.Start(CreateScene(args[0], itemId));
    }
    private IEnumerator CreateScene(string jsonFile, int itemId)
    {
        var frames = SceneDataLoader.LoadFromJson(jsonFile);
        var modelIds = new List<int>();
        var hiderIds = new List<int>();

        foreach (var frame in frames)
        {
            int clonedId = -1;
            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(itemId, frame.Position.ToVector3(), frame.Rotation.ToQuaternion(), (id) =>
            {
                clonedId = id;
            }));
            while (clonedId == -1)
                yield return null;

            modelIds.Add(clonedId);
        }

        MelonCoroutines.Start(CreateLogics(frames, hiderIds));

        while (hiderIds.Count < frames.Count)
            yield return null;

        for (int i = 0; i < modelIds.Count && i < hiderIds.Count; i++)
            WorldObjectOperations.AddObjectLink(hiderIds[i], modelIds[i]);
    }

    private IEnumerator CreateLogics(List<FrameData> frames, List<int> hiderIds)
    {
        MelonLogger.Msg("CreateLogics started");
        int mainId = -1;
        MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.DELAY_CUBE, new Vector3(0, 10, 0), Quaternion.identity, (id) =>
        {
            mainId = id;
        }));
        while (mainId == -1)
            yield return null;

        for (int i = 0; i < frames.Count; i++)
        {
            int currentId = -1;
            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.DELAY_CUBE, new Vector3(0, 0, i), Quaternion.identity, (id) =>
            {
                currentId = id;
            }));
            while (currentId == -1)
                yield return null;

            WorldObjectOperations.AddLink(mainId, currentId);
            WorldObjectOperations.SetProperty(currentId, "duration", frames[i].Duration);
            WorldObjectOperations.SetProperty(currentId, "time", frames[i].Duration * i);

            int currentModelHiderId = -1;
            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.CUBE_MODEL_HIDER, new Vector3(4, 0, i), Quaternion.identity, (id) =>
            {
                currentModelHiderId = id;
            }));
            while (currentModelHiderId == -1)
                yield return null;
            WorldObjectOperations.AddLink(currentId, currentModelHiderId);
            hiderIds.Add(currentModelHiderId);
        }
        float totalDuration = frames.Sum(f => f.Duration);
        WorldObjectOperations.SetProperty(mainId, "duration", totalDuration);
    }
}
