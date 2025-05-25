using UnityEngine;

public class UnitIdleTimer : MonoBehaviour
{
    public float Timer;
    private SimpleUnit unit;

    private void Awake()
    {
        unit = GetComponent<SimpleUnit>();
    }

    private void OnEnable()
    {
        Timer = 0f;
    }

    private void Update()
    {
        if (unit.IsIdle)
        {
            Timer += Time.deltaTime;
        }
        else 
        {
            Timer = 0f;
        }
    }
}
