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

    [JsonProperty("model")]
    public string ModelName { get; set; }

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

public class ChunkData
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("frames")]
    public List<FrameData> Frames { get; set; }
}


public static class SceneDataLoader
{
    public static List<ChunkData> LoadChunksFromJson(string filename)
    {
        string jsonPath = Path.Combine(MelonEnvironment.ModsDirectory, filename);
        if (!File.Exists(jsonPath))
            return new List<ChunkData>();
        string json = File.ReadAllText(jsonPath);
        return JsonConvert.DeserializeObject<List<ChunkData>>(json) ?? new List<ChunkData>();
    }
}