using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

/*** SELF IMPLEMENTED KNN (NOT USED AT THE MOMENT) ***/
public class KNN : MonoBehaviour
{

    private List<Tuple<double[], string>> trainingData;
    private static KNN instance;

    public static KNN Instance { 
        get {
            if (instance == null) {
                // To make sure that script is persistent across scenes
                GameObject go = new GameObject("KNN");
                instance = go.AddComponent<KNN>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    
    }

    public void Initialize(double[][] input, string[] labels) {
        if (input.Length != labels.Length) {
            throw new ArgumentException("Number of training data points must match number of labels.");
        }

        this.trainingData = new List<Tuple<double[], string>>();
        for (int i = 0; i < input.Length; i++) {
            this.trainingData.Add(Tuple.Create(input[i], labels[i]));
        }
    }

    /*** Performs KNN search and outputs label ***/
    public string Search(double[] query, int k = 3)
    {
        // Calculate distances to all training data points
        List<Tuple<double, string>> distances = new List<Tuple<double, string>>();

        foreach (var dataPoint in trainingData)
        {
            double distance = EuclideanDistance(query, dataPoint.Item1);
            distances.Add(Tuple.Create(distance, dataPoint.Item2));
        }

        // Sort distances and select k-nearest neighbors
        var sortedDistances = distances.OrderBy(d => d.Item1).Take(k);
        for (int i = 0; i < k; i++)
        {
            Debug.Log($"Distance: {sortedDistances.ElementAt(i).Item1}, Label: {sortedDistances.ElementAt(i).Item2}");
        }


        // Count occurrences of each class in the k-nearest neighbors
        var classCounts = sortedDistances.GroupBy(d => d.Item2)
                                         .Select(g => Tuple.Create(g.Count(), g.Key))
                                         .OrderByDescending(count => count.Item1);

        // Return the class with the highest count
        return classCounts.First().Item2;
    }

    private double DotProductDistance(double[] vector1, double[] vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vector dimensions must match.");

        double dotProduct = 0.0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
        }

        return dotProduct;
    }

    private double EuclideanDistance(double[] vector1, double[] vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vector dimensions must match.");

        double sum = 0.0;

        for (int i = 0; i < vector1.Length; i++)
        {
            double diff = vector1[i] - vector2[i];
            sum += diff * diff;
        }

        return Math.Sqrt(sum);
    }



}
