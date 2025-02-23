using System;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Player : MonoBehaviour {
    public Transform t;
    public PlayerUI playerUI;
    public DrawingSystem drawingSystem;
    public RatingSystem ratingSystem;
    public DialogueSystem dialogueSystem;
    public Camera mainCamera;
    private Transform mainCameraTransform;

    public CharacterController characterController;
    public FirstPersonController firstPersonController;

    private void Awake() {
        mainCameraTransform = mainCamera.transform;
    }

    private PaintingCanvas currentCanvas;
    private NPC currentNPC;
    private InteractiveObject currentObject;
    private InteractiveObject CurrentObject {
        get {
            return currentObject;
        }
        set {
            if(currentObject != value) {
                currentObject = value;
                playerUI.RefreshForObject(currentObject);
            }
        }
    }

    private void Update() {
        if(Time.frameCount % 6 == 0) {
            InteractiveObject nearestObject = InteractiveObjectManager.Instance.GetNearestInteractableObject(t.position, mainCameraTransform.forward);
            CurrentObject = nearestObject;
            currentCanvas = nearestObject as PaintingCanvas;
            currentNPC = nearestObject as NPC;
        }
        if (Input.GetKeyDown(KeyCode.E)) {
            if (currentCanvas != null) {
                SetFPSControllerActive(false);
                Action onExit = () => {
                    SetFPSControllerActive(true);
                };
                switch (currentCanvas.PaintingStatus) {
                    case PaintingStatus.Blank:
                    case PaintingStatus.NeedsFixing:
                        AudioManager.Instance.PlayStartDrawingSound(0.5f);
                        drawingSystem.DrawToCanvas(currentCanvas, mainCamera, onExit);
                        break;
                    case PaintingStatus.Complete:
                    case PaintingStatus.HallOfFame:
                        ratingSystem.EnterRatingMode(currentCanvas, mainCamera.transform, onExit);
                        break;
                }
            } else if (currentNPC != null) {
                if(currentNPC.speechBubble != null) {
                    currentNPC.speechBubble.Fade(false, false);
                }
                SetFPSControllerActive(false);
                dialogueSystem.TalkToNPC(currentNPC, mainCamera, () => {
                    SetFPSControllerActive(true);
                });
            }
        }
    }

    private void SetFPSControllerActive(bool isActive) {
        enabled = isActive;
        characterController.enabled = isActive;
        firstPersonController.enabled = isActive;
        playerUI.gameObject.SetActive(isActive);
        Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isActive;
    }
}
