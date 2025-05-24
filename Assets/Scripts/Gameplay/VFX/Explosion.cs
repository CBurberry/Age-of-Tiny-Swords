using UnityEngine;

public class Explosion : MonoBehaviour
{
    public void OnExplosionEnded()
    {
        Destroy(gameObject);
    }
}