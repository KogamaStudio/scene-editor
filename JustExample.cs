using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Il2Cpp;
using Il2CppMV.Common;
using Il2CppMV.WorldObject;
using Il2CppMV;
using Newtonsoft.Json;

// hell
// remember its just example code

namespace KoGaMaAnimation
{
    public class ObjectAnimationImporter
    {
        public class TransformFrame
        {
            public int id;
            public float dur;
            public Vector3 pos;
            public Quaternion rot;
        }
        private static bool _waitingForClone = false;
        private static int _lastClonedId = -1;
        private static bool _waitingForLogic = false;
        private static int _lastLogicId = -1;

        public static void ImportAnimation(int sourceModelId, string jsonPath)
        {
            if (!System.IO.File.Exists(jsonPath))
            {
                MelonLogger.Error("file not found");
                return;
            }

            var jsonContent = System.IO.File.ReadAllText(jsonPath);
            var frames = JsonConvert.DeserializeObject<List<TransformFrame>>(jsonContent);
            var originalObj = MVGameControllerBase.WOCM.GetWorldObjectClient(sourceModelId);

            if (originalObj == null)
            {
                MelonLogger.Error($"cant find object {sourceModelId}");
                return;
            }

            MelonCoroutines.Start(BuildRoutine(originalObj, frames));
        }

        private static IEnumerator BuildRoutine(MVWorldObjectClient originalObj, List<TransformFrame> frames)
        {
            MelonLogger.Msg($"starting clone sequence for {frames.Count} frames");

            int firstTriggerId = -1;
            int prevTriggerId = -1;

            var worldNetwork = MVGameControllerBase.Game.World.TryCast<WorldNetwork>();
            if (worldNetwork == null)
            {
                MelonLogger.Error("cant cast world to worldnetwork");
                yield break;
            }

            var wocmNetwork = worldNetwork.WorldObjectClientManagerNetwork;

            EventHandler<CloneWorldObjectTreeResponseEventArgs> cloneHandler = (sender, e) =>
            {
                if (e.Success)
                {
                    _lastClonedId = e.RootId;
                    _waitingForClone = false;
                }
            };
            wocmNetwork.CloneWorldObjectTreeResponse += cloneHandler;

            EventHandler<InitializedGameQueryDataEventArgs> logicHandler = (sender, e) =>
            {
                if (e.InstigatorActorNumber == MVGameControllerBase.Game.LocalPlayer.ActorNr)
                {
                    _lastLogicId = e.RootWO.Id;
                    _waitingForLogic = false;
                }
            };
            MVGameControllerBase.Game.World.InitializedGameQueryData += logicHandler;

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];

                _waitingForClone = true;
                MVGameControllerBase.OperationRequests.CloneWorldObjectTree(originalObj, true, false, false);

                float timeout = Time.time + 5.0f;
                while (_waitingForClone && Time.time < timeout) yield return null;

                if (_waitingForClone)
                {
                    MelonLogger.Error("clone timeout");
                    break;
                }

                int clonedModelId = _lastClonedId;

                var newPos = new Vector3(frame.pos.x, frame.pos.y, frame.pos.z);
                var newRot = new Quaternion(frame.rot.x, frame.rot.y, frame.rot.z, frame.rot.w);

                MVGameControllerBase.OperationRequests.UpdateWorldObject(clonedModelId, newPos, QuaternionCompression.ToBytes(newRot), TransformPackageType.Teleport);

                var clonedClient = MVGameControllerBase.WOCM.GetWorldObjectClient(clonedModelId);
                if (clonedClient != null)
                {
                    clonedClient.Position = newPos;
                    clonedClient.Rotation = newRot;
                }

                _waitingForLogic = true;
                CreateLogicItem(45, newPos + Vector3.up * 2);
                while (_waitingForLogic) yield return null;
                int togglerId = _lastLogicId;

                var objLink = new ObjectLink();
                objLink.objectConnectorWOID = togglerId;
                objLink.objectWOID = clonedModelId;
                MVGameControllerBase.OperationRequests.AddObjectLink(objLink);

                yield return new WaitForSeconds(0.1f);

                _waitingForLogic = true;
                CreateLogicItem(48, newPos + Vector3.up * 4);
                while (_waitingForLogic) yield return null;
                int triggerId = _lastLogicId;

                if (firstTriggerId == -1) firstTriggerId = triggerId;

                var updateData = new Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Object, Il2CppSystem.Object>();

                var key = (Il2CppSystem.Object)new Il2CppSystem.String("duration");
                var val = (Il2CppSystem.Object)new Il2CppSystem.Single { m_value = frame.dur }.BoxIl2CppObject();
                updateData.Add(key, val);

                MVGameControllerBase.OperationRequests.UpdateWorldObjectRunTimeData(triggerId, updateData);

                LinkLogic(triggerId, togglerId);

                if (prevTriggerId != -1)
                {
                    LinkLogic(prevTriggerId, triggerId);
                }
                prevTriggerId = triggerId;

                yield return new WaitForSeconds(0.2f);
            }

            if (prevTriggerId != -1 && firstTriggerId != -1)
            {
                LinkLogic(prevTriggerId, firstTriggerId);
            }

            wocmNetwork.CloneWorldObjectTreeResponse -= cloneHandler;
            MVGameControllerBase.Game.World.InitializedGameQueryData -= logicHandler;
            MelonLogger.Msg("import done");
        }

        private static void CreateLogicItem(int itemId, Vector3 pos)
        {
            MVGameControllerBase.OperationRequests.AddItemToWorld(
                itemId,
                MVGameControllerBase.WOCM.RootGroup.Id,
                pos,
                Quaternion.identity,
                true,
                false,
                false
            );
        }

        private static void LinkLogic(int outputId, int inputId)
        {
            var link = new Link();
            link.outputWOID = outputId;
            link.inputWOID = inputId;
            MVGameControllerBase.OperationRequests.AddLink(link);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
    public sealed class NullableAttribute : Attribute
    {
        public readonly byte[] NullableFlags;
        public NullableAttribute(byte flag)
        {
            NullableFlags = new byte[] { flag };
        }
        public NullableAttribute(byte[] flags)
        {
            NullableFlags = flags;
        }
    }
}
