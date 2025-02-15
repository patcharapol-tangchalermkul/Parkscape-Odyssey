using Niantic.Lightship.AR.Semantics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class SemanticQuerying : MonoBehaviour
{
    public ARCameraManager _cameraMan;
    public ARSemanticSegmentationManager _semanticMan;

    public TMP_Text _text;
    public RawImage _image;
    public Material _material;

    private string _channel = "ground";

    void OnEnable()
    {
        _cameraMan.frameReceived += OnCameraFrameUpdate;
    }

    private void OnDisable()
    {
        _cameraMan.frameReceived -= OnCameraFrameUpdate;
    }

    private void OnCameraFrameUpdate(ARCameraFrameEventArgs args)
    {
        if (!_semanticMan.subsystem.running)
        {
            return;
        }

        // get the semantic texture
        Matrix4x4 mat = Matrix4x4.identity;
        var texture = _semanticMan.GetSemanticChannelTexture(_channel, out mat);

        if (texture)
        {
            //the texture needs to be aligned to the screen so get the display matrix
            //and use a shader that will rotate/scale things.
            Matrix4x4 cameraMatrix = args.displayMatrix ?? Matrix4x4.identity;
            _image.material = _material;
            _image.material.SetTexture("_SemanticTex", texture);
            _image.material.SetMatrix("_SemanticMat", mat);
        }
    }

    private float _timer = 0.0f;

    void Update()
    {
        if (_semanticMan.subsystem == null || !_semanticMan.subsystem.running)
        {
            return;
        }

        //Unity Editor vs On Device
        if (Input.GetMouseButtonDown(0) || (Input.touches.Length > 0))
        {
            var pos = Input.mousePosition;

            if (pos.x > 0 && pos.x < Screen.width)
            {
                if (pos.y > 0 && pos.y < Screen.height)
                {
                    _timer += Time.deltaTime;
                    if (_timer > 0.05f)
                    {
                        string channelName = GetPositionChannel((int)pos.x, (int)pos.y);
                        // _text.text = channelName;
                        _timer = 0.0f;
                    }
                }
            }
        }
    }

    public string GetPositionChannel(int x, int y) 
    {
        var list = _semanticMan.GetChannelNamesAt(x, y);
        if (list.Count > 0) 
        {
            _channel = list[0];
            return _channel;
        }
        return "?";
    }
}