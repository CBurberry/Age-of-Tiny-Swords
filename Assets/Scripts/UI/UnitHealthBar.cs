using NaughtyAttributes;
using UnityEngine;

public class UnitHealthBar : MonoBehaviour
{
    private const float maxPositionDisplacement = -0.152f;
    private const float maxScale = 0.95f;

    [OnValueChanged("Inspector_OnAlphaChanged")]
    [SerializeField]
    [MinValue(0f)][MaxValue(1f)]
    private float alpha;

    [SerializeField]
    private SpriteRenderer fillRenderer;

    [SerializeField]
    private Transform fillTransform;

    public float GetValue() => alpha;
    public void SetValue(float alpha)
    {
        this.alpha = alpha;
        Vector3 tempPos = fillTransform.localPosition;
        Vector3 tempScale = fillTransform.localScale;
        tempPos.x = Mathf.Lerp(maxPositionDisplacement, 0f, alpha);
        tempScale.x = Mathf.Lerp(0f, maxScale, alpha);
        fillTransform.localPosition = tempPos;
        fillTransform.localScale = tempScale;
    }

    private void Inspector_OnAlphaChanged()
    {
        SetValue(alpha);
    }
}
