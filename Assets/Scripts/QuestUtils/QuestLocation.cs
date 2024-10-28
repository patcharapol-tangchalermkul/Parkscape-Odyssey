using System;
using Microsoft.Geospatial;
using UnityEngine;

[Serializable]
public class QuestLocation {
    public double LatitudeInDegrees {
        get => _latitude;
        set => _latitude = value;
    }

    public double LongitudeInDegrees {
        get => _longitude;
        set => _longitude = value;
    }

    [SerializeField]
    private double _latitude;
    
    [SerializeField]
    private double _longitude;

    public QuestLocation(LatLon coordinates) {
        LatitudeInDegrees = coordinates.LatitudeInDegrees;
        LongitudeInDegrees = coordinates.LongitudeInDegrees;
    }
}
