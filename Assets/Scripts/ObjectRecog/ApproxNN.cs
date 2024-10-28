using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using HNSW.Net;

public class ApproxNN : MonoBehaviour
{
    private const string VectorsPathSuffix = "locationQuestVectors.bytes";
    private const string LabelsPathSuffix = "locationQuestLabels.bytes";
    private const string GraphpathSuffix = "locationQuestGraph.bytes";
    private const float KNNThreshold = 0.5f;

    private SmallWorld<float[], float> world;
    public string[] labels;
    private float[][] vectors;
    private static ApproxNN instance;

    public static ApproxNN Instance { 
        get {
            if (instance == null) {
                // To make sure that script is persistent across scenes
                GameObject go = new GameObject("ApproxNN");
                instance = go.AddComponent<ApproxNN>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    
    }

    public void Initialize(float[][] input, string[] labels) {
        if (input.Length != labels.Length) {
            throw new ArgumentException("Number of training data points must match number of labels.");
        }

        var normalizedInputs = input.Select(NormalizeVector).ToArray();

        this.vectors = normalizedInputs;
        this.labels = labels;

        // Construct initial world
        var parameters = new SmallWorld<float[], float>.Parameters()
            {
                M = 15,
                LevelLambda = 1 / Math.Log(15),
            };
        // this.world = new SmallWorld<float[], float>(CosineDistance.ForUnits, DefaultRandomGenerator.Instance, parameters);
        // this.world.AddItems(normalizedInputs);
        this.world = new SmallWorld<float[], float>(CosineDistance.ForUnits);
        this.world.BuildGraph(normalizedInputs, new System.Random(42), parameters);
        Debug.Log("World constructed");
    }

    /*** Performs KNN search and outputs label ***/
    public string[] Search(float[] query, int k = 3)
    {
        float[] normalizedQuery = NormalizeVector(query);
        var results = this.world.KNNSearch(normalizedQuery, k);
        GameManager.Instance.LogTxt("All labels " + string.Join(", ", this.labels.Distinct().ToList()));
        GameManager.Instance.LogTxt("Labels " + string.Join(", ", results.Select(r => this.labels[r.Id])));
        GameManager.Instance.LogTxt("Distances " + string.Join(", ", results.Select(r => r.Distance)));

        var filteredResults = results.Where(r => r.Distance < KNNThreshold).ToArray();
        if (filteredResults.Length == 0) {
            return new string[0];
        }
        return WeightedNN(filteredResults, k);
    }
    
    /*** weighted nearest neighbour ***/
    private string[] WeightedNN(SmallWorld<float[], float>.KNNSearchResult[] results, int k) {
        var distances = results.Select(r => r.Distance).ToArray();
        var weights = distances.Select(d => 1 / (d + float.Epsilon)).ToArray();
        var labelWeights = new Dictionary<string, float>();
        for (int i = 0; i < results.Length; i++) {
            var label = this.labels[results[i].Id];
            if (labelWeights.ContainsKey(label)) {
                labelWeights[label] += weights[i];
            } else {
                labelWeights[label] = weights[i];
            }
        }
        GameManager.Instance.LogTxt("Label weights " + string.Join(", ", labelWeights.Select(kv => $"{kv.Key}: {kv.Value}")));
        var returnLabel = labelWeights.OrderByDescending(kv => kv.Value).First().Key;
        return new string[] { returnLabel };
        // return if number of occurence of label is greater than k // 2
        // if (results.Count(r => this.labels[r.Id] == returnLabel) > k / 2) {
        //     return new string[] { returnLabel };
        // } else {
        //     return new string[0];
        // }
    }

    public void Save(string path) {
        BinaryFormatter formatter = new BinaryFormatter();

        MemoryStream sampleVectorsStream = new MemoryStream();
        formatter.Serialize(sampleVectorsStream, vectors);
        File.WriteAllBytes($"{path}/{VectorsPathSuffix}", sampleVectorsStream.ToArray());

        MemoryStream labelsStream = new MemoryStream();
        formatter.Serialize(labelsStream, labels);
        File.WriteAllBytes($"{path}/{LabelsPathSuffix}", labelsStream.ToArray());

        using (var f = File.Open($"{path}/{GraphpathSuffix}", FileMode.Create))
        {
            byte[] buffer = world.SerializeGraph();
            f.Write(buffer, 0, buffer.Length);
        }
    }

    public void Load() {
        string path = DatabaseManager.Instance.dataPath + "quests/";
        Debug.Log("Loading world from " + path);
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream sampleVectorsStream = new MemoryStream(File.ReadAllBytes($"{path}/{VectorsPathSuffix}"));
        this.vectors = (float[][])formatter.Deserialize(sampleVectorsStream);

        MemoryStream labelsStream = new MemoryStream(File.ReadAllBytes($"{path}/{LabelsPathSuffix}"));
        this.labels = (string[])formatter.Deserialize(labelsStream);
        Debug.Log("Loaded labels");

        // using (var f = File.OpenRead($"{path}/{GraphpathSuffix}"))
        // {
        //     this.world = SmallWorld<float[], float>.DeserializeGraph(vectors, CosineDistance.ForUnits, DefaultRandomGenerator.Instance, f);
        // }
        MemoryStream graphStream = new MemoryStream(File.ReadAllBytes($"{path}/{GraphpathSuffix}"));
        byte[] buffer = graphStream.ToArray();
        Debug.Log("Loaded vectors");
        this.world = new SmallWorld<float[], float>(CosineDistance.ForUnits);
        Debug.Log("Loaded world");
        this.world.DeserializeGraph(this.vectors, buffer);
        Debug.Log("Deserialized graph");

        Debug.Log("World loaded");
    }

    private float[] NormalizeVector(float[] vector) {
        double l2Norm = Math.Sqrt(vector.Sum(x => x * x));
        return vector.Select(x => (float)(x / l2Norm)).ToArray();
    }
}
