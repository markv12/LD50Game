using System;
using System.Collections;
using UnityEngine;

public class NPC : InteractiveObject {
    public SpriteRenderer mainRenderer;
    public Sprite[] sprites;
    public float frameRate;
    public bool biDirectional = false;

    [TextArea(4, 12)]
    public string dialogueText;
    [Range(0.1f, 5f)]
    public float pitchCenter = 1;
    [TextArea(4, 12)]
    public string speechBubbleText;
    private string[] speechBubbleTexts;
    [Range(0f, 1f)]
    public float chanceOfSpeechBubbleText = 1;
    [NonSerialized] public SpeechBubble speechBubble;
    public override string InteractText => "Press 'E' to Talk";
    public override bool Interactable => !string.IsNullOrWhiteSpace(dialogueText);
    public override bool MustBeFacing => false;
    public override void OnNearChanged(bool isNear, Vector3 playerFaceDirection) {
        bool facingFront = biDirectional ? IsFacingFront(t.forward, playerFaceDirection) : true;
        string text = GetSpeechBubbleText(isNear);
        if(text != null) {
            SetSpeechBubbleActive(true, facingFront);
            speechBubble.SetText(text);
        } else {
            SetSpeechBubbleActive(false, facingFront);
        }
    }

    private bool IsFacingFront(Vector3 forward, Vector3 playerFaceDirection) {
        return Vector3.Dot(forward, playerFaceDirection) > 0;
    }

    private string GetSpeechBubbleText(bool isNear) {
        if (isNear && speechBubbleTexts.Length > 0) {
            if (UnityEngine.Random.Range(0f, 1f) < chanceOfSpeechBubbleText) {
                string text = speechBubbleTexts[UnityEngine.Random.Range(0, speechBubbleTexts.Length)];
                if (!string.IsNullOrWhiteSpace(text)) {
                    return text;
                }
            }
        }
        return null;
    }

    private void SetSpeechBubbleActive(bool active, bool facingFront) {
        if (active) {
            if(speechBubble == null) {
                speechBubble = ResourceManager.InstantiatePrefab<SpeechBubble>("SpeechBubble", Vector3.zero);
                speechBubble.transform.SetParent(transform, false);
                speechBubble.transform.localPosition = Vector3.zero;
            }
            speechBubble.Fade(true, facingFront);
        } else {
            if(speechBubble != null) {
                speechBubble.Fade(false, facingFront);
            }
        }
    }

protected override void Awake() {
        base.Awake();
        speechBubbleTexts = speechBubbleText.Split(DialogueSystem.DOUBLE_NEW_LINE, StringSplitOptions.None);
    }

    private Coroutine animateRoutine = null;
    private void OnEnable() {
        OnDisable();
        animateRoutine = StartCoroutine(Co_Animate());
    }

    private void OnDisable() {
        this.EnsureCoroutineStopped(ref animateRoutine);
    }

    IEnumerator Co_Animate() {
        yield return null;
        yield return null;
        yield return null;
        float timePerFrame = 1f / frameRate;
        float waitTime = UnityEngine.Random.Range(0f, timePerFrame);
        yield return new WaitForSeconds(waitTime);

        while (true) {
            for (int i = 0; i < sprites.Length; i++) {
                mainRenderer.sprite = sprites[i];
                float elapsedTime = 0;
                while (elapsedTime < timePerFrame) {
                    elapsedTime += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }
    }
}
