using AurecasLib.Blur;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PopupWindow : MonoBehaviour
{
    protected bool isOpen;
    protected bool inScene;
    private Animator animator;
    private Image backgroundImage;
    private Material blurMaterial;

    [HideInInspector]
    public BlurRenderer blurRenderer;

    public EventHandler OnOpen;
    public EventHandler OnClose;

    protected virtual void Awake() {
        animator = GetComponent<Animator>();
        inScene = true;
    }

    public virtual void SetBlurRenderer(BlurRenderer blurRenderer, Material mat) {
        this.blurRenderer = blurRenderer;
        backgroundImage = GetComponent<Image>();
        backgroundImage.material = blurMaterial = mat;
    }

    public void OpenPopup() {
        animator.Play("Open", 0, 0);
        OnOpen?.Invoke(this, null);
        isOpen = true;
    }

    public void ClosePopup() {
        animator.Play("Close", 0, 0);
        OnClose?.Invoke(this, null);
        isOpen = false;
    }

    public bool IsOpen() {
        return isOpen;
    }

    public bool InScene() {
        return inScene;
    }

    public virtual void OnBeginShow() { }
    public virtual void OnEndShow() { }
    public virtual void OnBeginClose() { }
    public virtual void OnEndClose() {
        inScene = false;
        blurRenderer.FreeBlur(blurMaterial);
    }

    public void OpenExternalLink(string link) {
        Application.OpenURL(link);
    }

}
