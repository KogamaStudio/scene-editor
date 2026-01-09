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
        var modelIds = new Dictionary<int, int>();
        var hiderIds = new List<int>();

        for (int i = 0; i < frames.Count; i++)
        {
            int idx = i;
            var frame = frames[i];
            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(itemId, frame.Position.ToVector3(), frame.Rotation.ToQuaternion(), (id) =>
            {
                modelIds[idx] = id;
            }));
            yield return new WaitForSeconds(0.1f);
        }

        while (modelIds.Count < frames.Count)
            yield return null;

        MelonCoroutines.Start(CreateLogics(frames, hiderIds));
        while (hiderIds.Count < frames.Count)
            yield return null;

        for (int i = 0; i < frames.Count; i++)
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

        var delayIds = new Dictionary<int, int>();
        var hiderIdDict = new Dictionary<int, int>();

        for (int i = 0; i < frames.Count; i++)
        {
            int idx = i;
            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.DELAY_CUBE, new Vector3(0, 0, i), Quaternion.identity, (id) =>
            {
                delayIds[idx] = id;
            }));
            yield return new WaitForSeconds(0.05f);
        }
        while (delayIds.Count < frames.Count)
            yield return null;

        for (int i = 0; i < frames.Count; i++)
        {
            int idx = i;
            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.CUBE_MODEL_HIDER, new Vector3(4, 0, i), Quaternion.identity, (id) =>
            {
                hiderIdDict[idx] = id;
            }));
            yield return new WaitForSeconds(0.05f);
        }
        while (hiderIdDict.Count < frames.Count)
            yield return null;

        for (int i = 0; i < frames.Count; i++)
        {
            WorldObjectOperations.AddLink(mainId, delayIds[i]);
            WorldObjectOperations.SetProperty(delayIds[i], "duration", frames[i].Duration);
            WorldObjectOperations.SetProperty(delayIds[i], "time", frames[i].Duration * i);
            WorldObjectOperations.AddLink(delayIds[i], hiderIdDict[i]);
            hiderIds.Add(hiderIdDict[i]);
        }

        float totalDuration = frames.Sum(f => f.Duration);
        WorldObjectOperations.SetProperty(mainId, "duration", totalDuration);
        WorldObjectOperations.SetProperty(mainId, "time", 0f);
    }
}
