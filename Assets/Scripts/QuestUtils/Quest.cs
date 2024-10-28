using UnityEngine;
using System;

public enum QuestType {
    FIND
}

public enum QuestStatus {
    NOT_STARTED,
    IN_PROGRESS,
    COMPLETED
}

[Serializable]
public class Quest
{
    // name of object to find
    public string Label {
        get => _label;
        private set => _label = value;
    }

    public Texture2D ReferenceImage {
        get => _referenceImage;
        private set => _referenceImage = value;
    }

    public QuestStatus QuestStatus {
        get => _questStatus;
        private set => _questStatus = value;
    }

    public QuestType QuestType {
        get => _questType;
        private set => _questType = value;
    }
    
    public int Progress {
        get => _progress;
        private set => _progress = value;
    }
    public int Target {
        get => _target;
        private set => _target = value;
    }


    // Private backing variables for properties so that this class can be properly serialized
    [SerializeField] private string _label;
    [SerializeField] private Texture2D _referenceImage;
    [SerializeField] private QuestStatus _questStatus;
    [SerializeField] private QuestType _questType;
    [SerializeField] private int _progress;
    [SerializeField] private int _target;

    public Quest(QuestType questType, string label, Texture2D referenceImage, int target)
    {
        QuestType = questType;
        Label = label;
        QuestStatus = QuestStatus.NOT_STARTED;
        Progress = 0;
        Target = target;
    }

    public void IncrementProgress() {
        if (Progress < Target)
            Progress++;
        if (Progress == Target)
            SetCompleted();
    }

    public void SetNotStarted() {
        QuestStatus = QuestStatus.NOT_STARTED;
    }

    public void SetOngoing() {
        QuestStatus = QuestStatus.IN_PROGRESS;
    }

    public void SetCompleted() {
        QuestStatus = QuestStatus.COMPLETED;
    }

    public bool IsOnGoing() {
        return QuestStatus == QuestStatus.IN_PROGRESS;
    }

    public bool IsCompleted() {
        return QuestStatus == QuestStatus.COMPLETED;
    }

    public bool HasNotStarted() {
        return QuestStatus == QuestStatus.NOT_STARTED;
    }
}
