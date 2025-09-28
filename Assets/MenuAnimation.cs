using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MenuAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public float defaultDuration = 0.3f;
    public AnimationCurve defaultCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Stagger Settings")]
    public float staggerDelay = 0.1f;
    public bool staggerChildren = true;
    
    private List<Coroutine> activeAnimations = new List<Coroutine>();
    
    // Fade animations
    public void FadeIn(CanvasGroup canvasGroup, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(FadeAnimation(canvasGroup, 0f, 1f, duration));
    }
    
    public void FadeOut(CanvasGroup canvasGroup, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(FadeAnimation(canvasGroup, 1f, 0f, duration));
    }
    
    private IEnumerator FadeAnimation(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = defaultCurve.Evaluate(progress);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
    }
    
    // Scale animations
    public void ScaleIn(Transform target, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(ScaleAnimation(target, Vector3.zero, Vector3.one, duration));
    }
    
    public void ScaleOut(Transform target, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(ScaleAnimation(target, Vector3.one, Vector3.zero, duration));
    }
    
    private IEnumerator ScaleAnimation(Transform target, Vector3 startScale, Vector3 endScale, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = defaultCurve.Evaluate(progress);
            
            target.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            yield return null;
        }
        
        target.localScale = endScale;
    }
    
    // Position animations
    public void SlideIn(RectTransform target, Vector2 startPos, Vector2 endPos, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(SlideAnimation(target, startPos, endPos, duration));
    }
    
    public void SlideOut(RectTransform target, Vector2 startPos, Vector2 endPos, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(SlideAnimation(target, startPos, endPos, duration));
    }
    
    private IEnumerator SlideAnimation(RectTransform target, Vector2 startPos, Vector2 endPos, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = defaultCurve.Evaluate(progress);
            
            target.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
            yield return null;
        }
        
        target.anchoredPosition = endPos;
    }
    
    // Size animations
    public void Resize(RectTransform target, Vector2 startSize, Vector2 endSize, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(ResizeAnimation(target, startSize, endSize, duration));
    }
    
    private IEnumerator ResizeAnimation(RectTransform target, Vector2 startSize, Vector2 endSize, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = defaultCurve.Evaluate(progress);
            
            target.sizeDelta = Vector2.Lerp(startSize, endSize, curveValue);
            yield return null;
        }
        
        target.sizeDelta = endSize;
    }
    
    // Staggered animations for children
    public void AnimateChildrenIn(Transform parent, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(StaggeredAnimation(parent, true, duration));
    }
    
    public void AnimateChildrenOut(Transform parent, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(StaggeredAnimation(parent, false, duration));
    }
    
    private IEnumerator StaggeredAnimation(Transform parent, bool animateIn, float duration)
    {
        List<Transform> children = new List<Transform>();
        
        // Get all direct children
        for (int i = 0; i < parent.childCount; i++)
        {
            children.Add(parent.GetChild(i));
        }
        
        // Animate each child with stagger
        for (int i = 0; i < children.Count; i++)
        {
            Transform child = children[i];
            
            if (animateIn)
            {
                // Start from invisible/scaled down
                child.localScale = Vector3.zero;
                if (child.GetComponent<CanvasGroup>() != null)
                {
                    child.GetComponent<CanvasGroup>().alpha = 0f;
                }
                
                // Animate in
                StartCoroutine(StaggeredChildAnimation(child, true, duration));
            }
            else
            {
                // Animate out
                StartCoroutine(StaggeredChildAnimation(child, false, duration));
            }
            
            // Wait for stagger delay
            if (i < children.Count - 1)
            {
                yield return new WaitForSeconds(staggerDelay);
            }
        }
    }
    
    private IEnumerator StaggeredChildAnimation(Transform child, bool animateIn, float duration)
    {
        Vector3 startScale = animateIn ? Vector3.zero : Vector3.one;
        Vector3 endScale = animateIn ? Vector3.one : Vector3.zero;
        
        float startAlpha = animateIn ? 0f : 1f;
        float endAlpha = animateIn ? 1f : 0f;
        
        CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = defaultCurve.Evaluate(progress);
            
            child.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            }
            
            yield return null;
        }
        
        child.localScale = endScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = endAlpha;
        }
    }
    
    // Bounce animation
    public void BounceIn(Transform target, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(BounceAnimation(target, true, duration));
    }
    
    public void BounceOut(Transform target, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(BounceAnimation(target, false, duration));
    }
    
    private IEnumerator BounceAnimation(Transform target, bool bounceIn, float duration)
    {
        Vector3 startScale = bounceIn ? Vector3.zero : Vector3.one;
        Vector3 endScale = bounceIn ? Vector3.one : Vector3.zero;
        
        float elapsedTime = 0f;
        float halfDuration = duration * 0.5f;
        
        // First half - scale up/out
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            
            Vector3 currentScale = Vector3.Lerp(startScale, endScale * 1.2f, progress);
            target.localScale = currentScale;
            
            yield return null;
        }
        
        elapsedTime = 0f;
        
        // Second half - settle to final scale
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            
            Vector3 currentScale = Vector3.Lerp(endScale * 1.2f, endScale, progress);
            target.localScale = currentScale;
            
            yield return null;
        }
        
        target.localScale = endScale;
    }
    
    // Utility methods
    public void StopAllAnimations()
    {
        foreach (Coroutine animation in activeAnimations)
        {
            if (animation != null)
            {
                StopCoroutine(animation);
            }
        }
        activeAnimations.Clear();
    }
    
    public void SetAnimationCurve(AnimationCurve newCurve)
    {
        defaultCurve = newCurve;
    }
    
    public void SetDefaultDuration(float duration)
    {
        defaultDuration = duration;
    }
    
    public void SetStaggerDelay(float delay)
    {
        staggerDelay = delay;
    }
}
