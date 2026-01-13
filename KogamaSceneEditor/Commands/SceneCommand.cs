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
        if (args.Length < 1)
        {
            TextCommand.NotifyUser("Usage: /scene <jsonfile>");
            return;
        }
        MelonCoroutines.Start(CreateScene(args[0]));
    }

    private IEnumerator CreateScene(string jsonFile)
    {
        var chunks = SceneDataLoader.LoadChunksFromJson(jsonFile);
        var allFrames = new List<FrameData>();
        var modelIds = new Dictionary<int, List<int>>();
        var hiderIds = new List<int>();

        foreach (var chunk in chunks)
        {
            if (!int.TryParse(chunk.Name.Split('.')[0], out int itemId))
            {
                MelonLogger.Msg($"Cannot parse itemId from chunk name: {chunk.Name}");
                continue;
            }

            foreach (var frame in chunk.Frames)
            {
                allFrames.Add(frame);

                if (!modelIds.ContainsKey(frame.Id))
                    modelIds[frame.Id] = new List<int>();

                int modelId = -1;
                MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(itemId, frame.Position.ToVector3(), frame.Rotation.ToQuaternion(), (id) =>
                {
                    modelId = id;
                    modelIds[frame.Id].Add(id);
                }));
                while (modelId == -1)
                    yield return null;
            }
        }

        MelonLogger.Msg($"Models created: {modelIds.Count}, Total frames: {allFrames.Count}");
        foreach (var kvp in modelIds)
        {
            MelonLogger.Msg($"modelIds[{kvp.Key}] = {kvp.Value}");
        }

        MelonLogger.Msg("Starting CreateLogics...");
        MelonCoroutines.Start(CreateLogics(allFrames, hiderIds, modelIds));
        while (hiderIds.Count < allFrames.Count)
            yield return null;
    }

    private IEnumerator CreateLogics(List<FrameData> frames, List<int> hiderIds, Dictionary<int, List<int>> modelIds)
    {
        MelonLogger.Msg("CreateLogics started");

        int mainId = -1;
        MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.DELAY_CUBE, new Vector3(0, 10, 0), Quaternion.identity, (id) =>
        {
            mainId = id;
        }));
        while (mainId == -1)
            yield return null;

        var frameGroups = frames.GroupBy(f => f.Id).ToList();
        var delayIds = new Dictionary<int, int>();
        var hiderIdDict = new Dictionary<int, int>();

        int groupIndex = 0;
        foreach (var group in frameGroups)
        {
            int idx = groupIndex;
            int frameId = group.Key;

            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.DELAY_CUBE, new Vector3(0, 0, idx), Quaternion.identity, (id) =>
            {
                delayIds[frameId] = id;
            }));
            yield return new WaitForSeconds(0.05f);

            groupIndex++;
        }
        while (delayIds.Count < frameGroups.Count)
            yield return null;

        groupIndex = 0;
        foreach (var group in frameGroups)
        {
            int idx = groupIndex;
            int frameId = group.Key;

            MelonCoroutines.Start(WorldObjectOperations.AddItemToWorld(ItemIdWWW.CUBE_MODEL_HIDER, new Vector3(4, 0, idx), Quaternion.identity, (id) =>
            {
                hiderIdDict[frameId] = id;
            }));
            yield return new WaitForSeconds(0.05f);

            groupIndex++;
        }
        while (hiderIdDict.Count < frameGroups.Count)
            yield return null;

        foreach (var group in frameGroups)
        {
            int frameId = group.Key;
            var firstFrame = group.First();

            WorldObjectOperations.AddLink(mainId, delayIds[frameId]);
            WorldObjectOperations.SetProperty(delayIds[frameId], "duration", firstFrame.Duration);
            WorldObjectOperations.SetProperty(delayIds[frameId], "time", firstFrame.Duration * (frameId - 1));
            WorldObjectOperations.AddLink(delayIds[frameId], hiderIdDict[frameId]);

            foreach (var frame in group)
            {
                foreach (var modelWorldId in modelIds[frame.Id])
                {
                    WorldObjectOperations.AddObjectLink(hiderIdDict[frameId], modelWorldId);
                }
            }

            hiderIds.Add(hiderIdDict[frameId]);
        }

        float totalDuration = frames.First().Duration * frameGroups.Count;
        WorldObjectOperations.SetProperty(mainId, "duration", totalDuration);
        WorldObjectOperations.SetProperty(mainId, "time", 0f);
    }
}
