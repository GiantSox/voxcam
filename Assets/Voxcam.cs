using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UI = UnityEngine.UI;

sealed class Voxcam : MonoBehaviour
{
    [SerializeField] Vector2Int _resolution = new Vector2Int(1280, 720);
    [SerializeField] int _decimation = 4;
    [SerializeField] float _tagSize = 0.05f;
    [SerializeField] Material _tagMaterial = null;
    [SerializeField] UI.RawImage _webcamPreview = null;
    [SerializeField] UI.RawImage _webcamPreview2 = null;
    [SerializeField] UI.Text _debugText = null;

    public Material voxcamMaterial;
    public float ZoomAmount = 1.5f;

    // Webcam input and buffer
    WebCamTexture _webcamRaw;
    RenderTexture _webcamBuffer;
    Color32 [] _readBuffer;

    // AprilTag detector and drawer
    AprilTag.TagDetector _detector;
    TagDrawer _drawer;

    public Camera cam;

    

    private LinkedList<Vector3> _pastPoses = new LinkedList<Vector3>();
    private Vector3 _lastPose = Vector3.zero;

    [SerializeField] private float yOffset;
    
    
    private CommandBuffer _renderCommands;

    void Start()
    {
        // Webcam initialization
        _webcamRaw = new WebCamTexture(_resolution.x, _resolution.y, 60);
        _webcamBuffer = new RenderTexture(_resolution.x, _resolution.y, 0);
        _readBuffer = new Color32 [_resolution.x * _resolution.y];

        _webcamRaw.Play();
        _webcamPreview.texture = _webcamBuffer;
        _webcamPreview2.texture = _webcamBuffer;

        // Detector and drawer
        _detector = new AprilTag.TagDetector(_resolution.x, _resolution.y, _decimation);
        _drawer = new TagDrawer(_tagMaterial);

        _renderCommands = new CommandBuffer();
        //_renderCommands.Blit(null, null as RenderTexture, voxcamMaterial);
        //cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _renderCommands);
    }

    void OnDestroy()
    {
        Destroy(_webcamRaw);
        Destroy(_webcamBuffer);

        _detector.Dispose();
        _drawer.Dispose();
    }

    private float sum;
    //private uint index;
    const uint numSamples = 60;
    
    void sma()
    {
        //sum += 
        
        var output = (sum + ((float) numSamples / 2)) / numSamples;
    }

    //this is really not the best way to implement this but it works for now
    Vector3 stupidMovingAverage(Vector3 current, LinkedList<Vector3> list)
    {
        if(list.Count >= 120)
            list.RemoveFirst();
        
        list.AddLast(current);

        Vector3 avg = Vector3.zero;
        for (var i = list.First; i != null; i = i.Next)
        {
            avg += i.Value / 120;
        }

        return avg;
    }

    void Update()
    {
        // Check if the webcam is ready (needed for macOS support)
        if (_webcamRaw.width <= 16) return;

        // Check if the webcam is flipped (needed for iOS support)
        if (_webcamRaw.videoVerticallyMirrored)
        {
            _webcamPreview.transform.localScale = new Vector3(1, -1, 1);
            _webcamPreview2.transform.localScale = new Vector3(1, -1, 1);
        }

        // Webcam image buffering
        _webcamRaw.GetPixels32(_readBuffer);
        Graphics.Blit(_webcamRaw, _webcamBuffer);

        // AprilTag detection
        var fov = GetComponent<Camera>().fieldOfView * Mathf.Deg2Rad;
        _detector.ProcessImage(_readBuffer, fov, _tagSize);

        voxcamMaterial.mainTexture = _webcamRaw;

        // Detected tag visualization
        int count = 0;
        foreach (var tag in _detector.DetectedTags)
        {
            count++;
            //_drawer.Draw(tag.ID, tag.Position, tag.Rotation, _tagSize);

            var trs = Matrix4x4.TRS(tag.Position, tag.Rotation, Vector3.one * _tagSize);

            //var basePos = new Vector4(0f, 0f, 0f, 1);
            //var pos3D = trs * basePos;
            var pos3D = tag.Position;
            pos3D.y -= yOffset;
            //var worldPosSmoothed = stupidMovingAverage(worldPos, _pastPoses);
            //Vector4 v4WorldPosSmoothed = worldPosSmoothed;
            //v4WorldPosSmoothed.w = 1;
            
            // var viewPos = cam.worldToCameraMatrix * worldPos;   //really we should skip this step
            //var clipPos = cam.projectionMatrix * worldPos;
            //var ndcPos = clipPos / clipPos.w;
            //
            //Vector3 screenPos2 = new Vector2(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f);
            var screenPos2 = cam.WorldToViewportPoint(pos3D);
            
            //This may look a little weird -- we get better results if we apply the smoothing
            //to the screenspace position, but we're using the tag depth as the zoom factor,
            //which we also want to smooth. 
            screenPos2.z = tag.Position.z;
            _lastPose = screenPos2;
            
            //Graphics.DrawMesh(_drawer._mesh, trs, _tagMaterial, 0);
        }
        
        var targetPos = stupidMovingAverage(_lastPose, _pastPoses);
        //var targetPos = screenPos2;
            
        Shader.SetGlobalFloat("_centerX", targetPos.x);
        Shader.SetGlobalFloat("_centerY", targetPos.y);
        Shader.SetGlobalFloat("_zoomAmount", 1/(ZoomAmount * targetPos.z));

        Debug.Log("detected: " + count);

        //Profile data output (with 30 frame interval)
        if (Time.frameCount % 30 == 0)
            _debugText.text = _detector.ProfileData.Aggregate
              ("Profile (usec)", (c, n) => $"{c}\n{n.name} : {n.time}");
    }
}
