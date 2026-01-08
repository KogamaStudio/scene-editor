using MelonLoader;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq; 
using Newtonsoft.Json;
using Il2Cpp;
using Il2CppMV.Common;
using Il2CppMV.WorldObject;
using System;
using SysGen = System.Collections.Generic; 
using Il2CppGen = Il2CppSystem.Collections.Generic;
using Il2CppSys = Il2CppSystem;

[assembly: MelonInfo(typeof(KogamaAnimTool.AnimImporter), "Kogama StopMotion Importer", "1.2.0", "Veni & amuarte")]
[assembly: MelonGame("Kogama", "Kogama")]

namespace KogamaAnimTool
{
    public class Voxel 
    { 
        public short x, y, z; 
        public byte m; 
    }

    public class FrameData 
    { 
        public int id; 
        public float dur; 
        public SysGen.List<Voxel> cubes; 
    }

    public class AnimImporter : MelonMod
    {
        private string jsonPath = @"C:\KogamaExport\anim_data.json";
        private bool waitingForId = false;
        private int lastSpawnedId = -1;
        private int myActorNr = 0;
        private Il2CppSys.EventHandler<InitializedGameQueryDataEventArgs> spawnHandler;

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                if (!File.Exists(jsonPath))
                {
                    MelonLogger.Error("file missing bro: " + jsonPath);
                    return;
                }

                try
                {
                    string txt = File.ReadAllText(jsonPath);
                    var data = JsonConvert.DeserializeObject<SysGen.List<FrameData>>(txt);
                    MelonCoroutines.Start(BuildAnim(data));
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"json dead: {e.Message}");
                }
            }
        }

        private IEnumerator BuildAnim(SysGen.List<FrameData> frames)
        {
            MelonLogger.Msg($"starting import for {frames.Count} frames");
            
            var game = MVGameControllerBase.Game;
            var wocm = MVGameControllerBase.WOCM;
            var logicalPlayer = game.LocalPlayer;
            if (logicalPlayer == null) 
            {
                MelonLogger.Error("player null wth");
                yield break;
            }
            myActorNr = logicalPlayer.ActorNr;
            int avatarId = logicalPlayer.WoId;
            var avatarObj = wocm.GetWorldObjectClient(avatarId);
            var avatarLocal = avatarObj?.TryCast<MVAvatarLocal>();

            if (avatarLocal == null)
            {
                MelonLogger.Error("cant find avatar local instance");
                yield break;
            }
            Transform pTr = avatarLocal.transform;
            Vector3 basePos = pTr.position + (pTr.forward * 5);
            Vector3 logicPos = basePos + (pTr.right * 5);
            spawnHandler = new Action<Il2CppSys.Object, InitializedGameQueryDataEventArgs>(OnSpawned)
                .CastDelegate<Il2CppSys.EventHandler<InitializedGameQueryDataEventArgs>>();
                
            game.World.InitializedGameQueryData += spawnHandler;
            int trigItem = 48; 
            int togItem = 45;

            int firstTrigId = -1;
            int prevTrigId = -1;

            for (int i = 0; i < frames.Count; i++)
            {
                var f = frames[i];
                Vector3 currentLogicPos = logicPos + (Vector3.right * (i * 2)); 
                MelonLogger.Msg($"building frame {i+1}");
                waitingForId = true;
                var props = new Il2CppGen.Dictionary<Il2CppSys.Object, Il2CppSys.Object>();
                props.Add(new Il2CppSys.Byte((byte)1), new Il2CppSys.Single(1.0f)); 
                props.Add(new Il2CppSys.Byte((byte)3), new Il2CppSys.Int32(logicalPlayer.ProfileID)); 

                MVGameControllerBase.OperationRequests.RequestBuiltInItem(
                    BuiltInItem.CubeModel,
                    wocm.RootGroup.Id,
                    props,
                    basePos,
                    Quaternion.identity,
                    Vector3.one,
                    true, false
                );

                while (waitingForId) yield return null;
                int modelId = lastSpawnedId;
                yield return InjectVoxels(modelId, f.cubes);
                waitingForId = true;
                MVGameControllerBase.OperationRequests.AddItemToWorld(
                    togItem,
                    wocm.RootGroup.Id,
                    currentLogicPos + Vector3.forward,
                    Quaternion.identity,
                    true, false, false
                );
                
                while (waitingForId) yield return null;
                int togId = lastSpawnedId;
                var objLink = new ObjectLink();
                objLink.objectConnectorWOID = togId;
                objLink.objectWOID = modelId;
                objLink.id = 0; 
                MVGameControllerBase.OperationRequests.AddObjectLink(objLink);

                yield return new WaitForSeconds(0.1f);
                waitingForId = true;
                MVGameControllerBase.OperationRequests.AddItemToWorld(
                    trigItem,
                    wocm.RootGroup.Id,
                    currentLogicPos,
                    Quaternion.identity,
                    true, false, false
                );

                while (waitingForId) yield return null;
                int trigId = lastSpawnedId;

                if (firstTrigId == -1) firstTrigId = trigId;
                var runData = new Il2CppGen.Dictionary<string, Il2CppSys.Object>();
                runData.Add("duration", new Il2CppSys.Single(f.dur));
                var genericData = runData.Cast<Il2CppGen.Dictionary<Il2CppSys.Object, Il2CppSys.Object>>();
                MVGameControllerBase.OperationRequests.UpdateWorldObjectRunTimeData(trigId, genericData);
                LinkLogic(trigId, togId);

                if (prevTrigId != -1)
                {
                    LinkLogic(prevTrigId, trigId);
                }

                prevTrigId = trigId;
            }
            if (prevTrigId != -1 && firstTrigId != -1)
            {
                LinkLogic(prevTrigId, firstTrigId);
            }
            game.World.InitializedGameQueryData -= spawnHandler;
            MelonLogger.Msg("job done animation ready");
        }

        private void OnSpawned(Il2CppSys.Object sender, InitializedGameQueryDataEventArgs e)
        {
            if (e.InstigatorActorNumber == myActorNr)
            {
                lastSpawnedId = e.RootWO.Id;
                waitingForId = false;
            }
        }

        private IEnumerator InjectVoxels(int mid, SysGen.List<Voxel> voxels)
        {
            var obj = MVGameControllerBase.WOCM.GetWorldObjectClient(mid);
            if (obj == null) yield break;

            var model = obj.TryCast<MVCubeModelInstance>();
            if (model == null) yield break;

            model.MakeUnique();
            
            int counter = 0;
            int batch = 80; 

            foreach (var v in voxels)
            {
                var mats = new byte[6];
                for(int k=0; k<6; k++) mats[k] = v.m;

                var cube = new Cube(CubeBase.IdentityByteCorners, mats);
                model.AddCube(new IntVector(v.x, v.y, v.z), cube);

                counter++;
                if (counter >= batch)
                {
                    counter = 0;
                    model.HandleDelta();
                    yield return new WaitForSeconds(0.5f); 
                }
            }
            model.HandleDelta();
            yield return new WaitForSeconds(0.1f);
        }

        private void LinkLogic(int outId, int inId)
        {
            var link = new Link();
            link.outputWOID = outId;
            link.inputWOID = inId;
            MVGameControllerBase.OperationRequests.AddLink(link);
        }
    }
    public static class DelegateExtensions
    {
        public static T CastDelegate<T>(this Delegate source) where T : class
        {
            if (source == null) return null;
            Delegate[] delegates = source.GetInvocationList();
            if (delegates.Length == 1)
                return Delegate.CreateDelegate(typeof(T), delegates[0].Target, delegates[0].Method) as T;
            Delegate[] delegatesDest = new Delegate[delegates.Length];
            for (int nDelegate = 0; nDelegate < delegates.Length; nDelegate++)
                delegatesDest[nDelegate] = Delegate.CreateDelegate(typeof(T), delegates[nDelegate].Target, delegates[nDelegate].Method);
            return Delegate.Combine(delegatesDest) as T;
        }
    }
}
