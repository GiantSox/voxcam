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
    [SerializeField] UI.Text _debugText = null;

    public Material voxcamMaterial;
    
    public float xshift = 0;
    public float yshift = 0;
    
    // Webcam input and buffer
    WebCamTexture _webcamRaw;
    RenderTexture _webcamBuffer;
    Color32 [] _readBuffer;

    // AprilTag detector and drawer
    AprilTag.TagDetector _detector;
    TagDrawer _drawer;

    public Camera cam;

    private CommandBuffer _renderCommands;

    void Start()
    {
        // Webcam initialization
        _webcamRaw = new WebCamTexture(_resolution.x, _resolution.y, 60);
        _webcamBuffer = new RenderTexture(_resolution.x, _resolution.y, 0);
        _readBuffer = new Color32 [_resolution.x * _resolution.y];

        _webcamRaw.Play();
        _webcamPreview.texture = _webcamBuffer;

        // Detector and drawer
        _detector = new AprilTag.TagDetector(_resolution.x, _resolution.y, _decimation);
        _drawer = new TagDrawer(_tagMaterial);

        _renderCommands = new CommandBuffer();
        _renderCommands.Blit(null, null as RenderTexture, voxcamMaterial);
        cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _renderCommands);
    }

    void OnDestroy()
    {
        Destroy(_webcamRaw);
        Destroy(_webcamBuffer);

        _detector.Dispose();
        _drawer.Dispose();
    }

    void Update()
    {
        // Check if the webcam is ready (needed for macOS support)
        if (_webcamRaw.width <= 16) return;

        // Check if the webcam is flipped (needed for iOS support)
        if (_webcamRaw.videoVerticallyMirrored)
            _webcamPreview.transform.localScale = new Vector3(1, -1, 1);

        // Webcam image buffering
        _webcamRaw.GetPixels32(_readBuffer);
        Graphics.Blit(_webcamRaw, _webcamBuffer);

        // AprilTag detection
        var fov = GetComponent<Camera>().fieldOfView * Mathf.Deg2Rad;
        _detector.ProcessImage(_readBuffer, fov, _tagSize);

        voxcamMaterial.mainTexture = _webcamRaw;

        // Detected tag visualization
        foreach (var tag in _detector.DetectedTags)
        {
            //_drawer.Draw(tag.ID, tag.Position, tag.Rotation, _tagSize);

            var trs = Matrix4x4.TRS(tag.Position, tag.Rotation, Vector3.one * _tagSize);

            var basePos = new Vector4(0f, 0f, 0f, 1);
            var worldPos = trs * basePos;
            var viewPos = cam.worldToCameraMatrix * worldPos;   //really we should skip this step
            var clipPos = cam.projectionMatrix * viewPos;
            var ndcPos = clipPos / clipPos.w;

            var screenPos = new Vector2(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f);
            var screenPos2 = cam.WorldToViewportPoint(trs * basePos);
            
            Shader.SetGlobalFloat("centerX", screenPos2.x);
            Shader.SetGlobalFloat("centerY", screenPos2.y);
            Graphics.DrawMesh(_drawer._mesh, trs, _tagMaterial, 0);
        }
        

        //Profile data output (with 30 frame interval)
        if (Time.frameCount % 30 == 0)
            _debugText.text = _detector.ProfileData.Aggregate
              ("Profile (usec)", (c, n) => $"{c}\n{n.name} : {n.time}");
    }
}
