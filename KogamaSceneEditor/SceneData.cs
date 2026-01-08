using MelonLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KogamaSceneEditor;

public class FrameData
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("dur")]
    public float Duration { get; set; }

    [JsonProperty("pos")]
    public Vector3Data Position { get; set; }

    [JsonProperty("rot")]
    public QuaternionData Rotation { get; set; }
}

public class Vector3Data
{
    [JsonProperty("x")]
    public float X { get; set; }

    [JsonProperty("y")]
    public float Y { get; set; }

    [JsonProperty("z")]
    public float Z { get; set; }

    public Vector3 ToVector3() => new Vector3(X, Y, Z);
}

public class QuaternionData
{
    [JsonProperty("x")]
    public float X { get; set; }

    [JsonProperty("y")]
    public float Y { get; set; }

    [JsonProperty("z")]
    public float Z { get; set; }

    [JsonProperty("w")]
    public float W { get; set; }

    public Quaternion ToQuaternion() => new Quaternion(X, Y, Z, W);
}

public static class SceneDataLoader
{
    public static List<FrameData> LoadFromJson(string filename)
    {
        string jsonPath = Path.Combine(MelonEnvironment.ModsDirectory, filename);
        if (!File.Exists(jsonPath))
            return new List<FrameData>();

        string json = File.ReadAllText(jsonPath);
        return JsonConvert.DeserializeObject<List<FrameData>>(json) ?? new List<FrameData>();
    }
}