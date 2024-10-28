using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.ObjectDetection;
using Niantic.Lightship.AR.Subsystems.ObjectDetection;
using Niantic.Lightship.AR.XRSubsystems;
using TMPro;
using UnityEngine;

public class ObjectDetectionManager : MonoBehaviour
{
    [SerializeField]
    private ARObjectDetectionManager _objectDetectionManager;

    [SerializeField]
    private GameObject gameManagerObj;
    private GameManager gameManager;

    private Dictionary<string, float> objectDetectionTimes = new();
    private const float detectionTTL = 2f;  // seconds
    private const float confidenceThreshold = 0.7f;

    private void Start()
    {
        _objectDetectionManager.enabled = true;
        _objectDetectionManager.MetadataInitialized += OnMetadataInitialized;

        gameManager = gameManagerObj.GetComponent<GameManager>();
    }

    private void OnMetadataInitialized(ARObjectDetectionModelEventArgs args)
    {
        _objectDetectionManager.ObjectDetectionsUpdated += ObjectDetectionsUpdated;
    }

    private void ObjectDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
    {
        //Initialize our output string
        string resultString = "";
        var result = args.Results;

        if (result == null)
        {
            Debug.Log("No results found.");
            return;
        }

        //Reset our results string
        resultString = "";

        //Iterate through our results
        for (int i = 0; i < result.Count; i++)
        {
            var detection = result[i];
            var categorizations = detection.GetConfidentCategorizations();
            if (categorizations.Count <= 0)
            {
                break;
            }

            //Sort our categorizations by highest confidence
            categorizations.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));

            //Iterate through found categoires and form our string to output
            for (int j = 0; j < categorizations.Count; j++)
            {
                var categoryToDisplay = categorizations[j];

                resultString += "Detected " + $"{categoryToDisplay.CategoryName}: " + "with " + $"{categoryToDisplay.Confidence} Confidence \n";

                // Add object to category.
                if (categoryToDisplay.Confidence >= confidenceThreshold) {
                    AddToDetectionTimes(categoryToDisplay.CategoryName);
                }
            }
        }

        //Output our string
        // gameManager.RelogTxt(resultString);
    }
    private void OnDestroy()
    {
        _objectDetectionManager.MetadataInitialized -= OnMetadataInitialized;
        _objectDetectionManager.ObjectDetectionsUpdated -= ObjectDetectionsUpdated;
    }

    public List<string> GetLabels()
    {
        List<string> labels = new();
        foreach (var pair in objectDetectionTimes)
        {
            if (Time.time - pair.Value <= detectionTTL) { // if the object was detected within the last 3 seconds
                labels.Add(pair.Key);
            }
        }
        return labels;
    }

    public List<string> GetAllLabels()
    {
        return objectDetectionTimes.Keys.ToList();
    }

    private void AddToDetectionTimes(string label)
    {
        if (objectDetectionTimes.ContainsKey(label))
        {
            objectDetectionTimes[label] = Time.time;
        }
        else
        {
            objectDetectionTimes.Add(label, Time.time);
        }
    }
}

