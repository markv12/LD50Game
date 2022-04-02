using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DrawingSystem : MonoBehaviour {
    public DrawingBrush brush;
    public RenderTexture RTexture;

    public Camera renderCamera;
    public Transform renderCameraTransform;

    public GameObject drawingUI;

    public LayerMask layerMask;

    private PaintingCanvas currentCanvas;
    private Action onExit;

    private Camera mainCamera;
    private Transform mainCameraTransform;
    private Vector3 mainCameraOriginalPos;
    private Quaternion mainCameraOriginalRotation;

    private bool inDrawingMode = false;
    private bool InDrawingMode {
        get {
            return inDrawingMode;
        }
        set {
            inDrawingMode = value;
            drawingUI.SetActive(inDrawingMode);
        }
    }

    private int brushSize = 1;
    public int BrushSize {
        get {
            return brushSize;
        }
        set {
            brushSize = Mathf.Max(0, Mathf.Min(5, value));
        }
    }

    private Color brushColor = Color.white;
    public Color BrushColor {
        get {
            return brushColor;
        }
        set {
            brushColor = value;
        }
    }

    private static readonly float[] BRUSH_SIZES = new float[] { 0.025f, 0.05f, 0.1f, 0.3f, 0.5f, 1f };

    public void DrawToCanvas(PaintingCanvas paintingCanvas, Camera _mainCamera, Action _onExit) {
        currentCanvas = paintingCanvas;
        mainCamera = _mainCamera;
        mainCameraTransform = _mainCamera.transform;
        onExit = _onExit;

        mainCameraOriginalPos = mainCameraTransform.localPosition;
        mainCameraOriginalRotation = mainCameraTransform.localRotation;

        MoveCameraInFrontOfCanvas();
    }

    private readonly List<DrawingBrush> brushInstances = new List<DrawingBrush>();
    private bool isDrawing = false;
    void Update() {
        if (InDrawingMode) {
            if (Input.GetMouseButtonDown(0)) {
                if (isDrawing == false) {
                    AudioManager.Instance.PlayBrushSound(UnityEngine.Random.Range(0.1f, 0.4f));
                }
                isDrawing = true;
            } else if (Input.GetMouseButtonUp(0)) {
                isDrawing = false;
                lastDrawPosition = Vector3.zero;
                WriteBrushesToTexture();
            }

            if (isDrawing) {
                Draw();
            }
        }
    }

    private void WriteBrushesToTexture() {
        StartCoroutine(WriteRoutine());

        IEnumerator WriteRoutine() {
            renderCamera.Render();
            currentCanvas.SetCanvasTexture(RTexture);
            yield return null;
            Clear();
        }
    }

    private Vector3 lastDrawPosition;
    private const float MAX_DOT_DISTANCE = 0.025f;
    private void Draw() {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 10, layerMask)) {
            Transform canvasT = hit.collider.transform;
            Vector3 spawnPoint = hit.point + (canvasT.forward * -0.005f);
            float diffMagnitude = (spawnPoint - lastDrawPosition).magnitude;
            if (diffMagnitude > MAX_DOT_DISTANCE && lastDrawPosition != Vector3.zero) {
                FillInBetween(lastDrawPosition, spawnPoint, canvasT.rotation, diffMagnitude);
            }
            DrawDot(spawnPoint, canvasT.rotation);
            lastDrawPosition = spawnPoint;
        }
    }

    private void FillInBetween(Vector3 startPos, Vector3 endPos, Quaternion spawnRotation, float diffMagnitude) {
        int dotsToDraw = Mathf.CeilToInt(diffMagnitude / MAX_DOT_DISTANCE);
        for (int i = 1; i < dotsToDraw; i++) {
            float progress = (float)i / (float)dotsToDraw;
            Vector3 drawPos = Vector3.Lerp(startPos, endPos, progress);
            DrawDot(drawPos, spawnRotation);
        }
    }

    private void DrawDot(Vector3 spawnPoint, Quaternion spawnRotation) {
        DrawingBrush brushInstance = Instantiate(brush, spawnPoint, spawnRotation, transform);
        brushInstance.transform.localScale = Vector3.one * BRUSH_SIZES[BrushSize];
        brushInstance.SetColor(BrushColor);
        brushInstances.Add(brushInstance);
    }

    public const float ANIM_DURATION = 0.8f;
    private void MoveCameraInFrontOfCanvas() {
        Vector3 startPos = mainCameraTransform.position;
        Quaternion startRotation = mainCameraTransform.rotation;
        Vector3 endPos = currentCanvas.transform.position + (currentCanvas.transform.forward * -2.05f);
        Quaternion endRotation = Quaternion.LookRotation(currentCanvas.transform.forward, currentCanvas.transform.up);
        renderCamera.transform.SetPositionAndRotation(endPos, endRotation);
        this.CreateAnimationRoutine(ANIM_DURATION, (float progress) => {
            float easedProgress = Easing.easeInOutSine(0f, 1f, progress);
            mainCameraTransform.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, easedProgress), Quaternion.Lerp(startRotation, endRotation, easedProgress));
        }, () => {
            InDrawingMode = true;
        });
    }

    public void ReturnCameraToPlayer() {
        InDrawingMode = false;
        isDrawing = false;
        Vector3 startPos = mainCameraTransform.localPosition;
        Quaternion startRotation = mainCameraTransform.localRotation;
        Vector3 endPos = mainCameraOriginalPos;
        this.CreateAnimationRoutine(ANIM_DURATION, (float progress) => {
            float easedProgress = Easing.easeInOutSine(0f, 1f, progress);
            mainCameraTransform.localPosition = Vector3.Lerp(startPos, mainCameraOriginalPos, easedProgress);
            mainCameraTransform.localRotation = Quaternion.Lerp(startRotation, mainCameraOriginalRotation, easedProgress);
        }, () => {
            onExit?.Invoke();
        });
    }

    public void SubmitCurrentDrawing() {
        Texture2D tex = currentCanvas.canvasSpriteRenderer.sprite.texture;
        switch (currentCanvas.PaintingStatus) {
            case PaintingStatus.Blank:
                ArtNetworking.Instance.SendUnfinishedImage(tex);
                break;
            case PaintingStatus.NeedsFixing:
                ArtNetworking.Instance.SendFinishedImage(tex, currentCanvas.ImageID);
                break;
        }
        ReturnCameraToPlayer();
    }

    public void Clear() {
        for (int i = 0; i < brushInstances.Count; i++) {
            Destroy(brushInstances[i].gameObject);
        }
        brushInstances.Clear();
    }

    public void Save() {
        StartCoroutine(CoSave());
    }

    private IEnumerator CoSave() {
        //wait for rendering
        yield return new WaitForEndOfFrame();
        Debug.Log(Application.dataPath + "/savedImage.png");

        //set active texture
        RenderTexture.active = RTexture;

        //convert rendering texture to texture2D
        var texture2D = new Texture2D(RTexture.width, RTexture.height);
        texture2D.ReadPixels(new Rect(0, 0, RTexture.width, RTexture.height), 0, 0);
        texture2D.Apply();

        //write data to file
        var data = texture2D.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/savedImage.png", data);
    }
}
