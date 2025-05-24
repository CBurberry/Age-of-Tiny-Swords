using UnityEngine;

public class Arrow : AProjectile
{
    private Vector3 startPos;
    private float alpha;

    private void OnEnable()
    {
        alpha = 0f;
        startPos = transform.position;
        SpriteRenderer.enabled = true;
    }

    private void Update()
    {
        if (alpha < 1f)
        {
            alpha = Mathf.Clamp(alpha + (Time.deltaTime * Speed), 0f, 1f);
            transform.position = startPos + (TravelVector * alpha);
        }
        else 
        {
            SpriteRenderer.enabled = false;
            OnComplete?.Invoke();
        }
    }
}
