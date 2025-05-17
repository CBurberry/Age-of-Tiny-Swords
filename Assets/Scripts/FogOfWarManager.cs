using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Burst.Intrinsics.X86.Avx;

public class FogOfWarManager : MonoBehaviour
{
    [SerializeField] RenderTexture _cameraRenderTexture;
    [SerializeField] RenderTexture _fogTexture;
    [SerializeField] RenderTexture _currentlyRevealedArea;
    [SerializeField] Material _drawMaterial;
    [SerializeField] float _revealRadius = 1f;

    CommandBuffer _commandBuffer;

    Camera _camera;

    void Awake()
    {
        _camera = Camera.main;
        _commandBuffer = new CommandBuffer();

        RenderTexture.active = _fogTexture;
        GL.Clear(true, true, Color.black);
        
        RenderTexture.active = _currentlyRevealedArea;
        GL.Clear(true, true, Color.black); 
        
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
    }

    void LateUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            List<Vector3> revealPositions = new() { worldPos };
            RevealPositions(revealPositions);
        }
    }

    public void RevealPositions(List<Vector3> revealPositions)
    {
        float sizeOfMap = 100f;
        _commandBuffer.Clear();
        _commandBuffer.SetRenderTarget(_currentlyRevealedArea);
        _commandBuffer.ClearRenderTarget(true, true, Color.black);

        for (int i = 0; i < revealPositions.Count; i++)
        {
            var position = revealPositions[i];
            Vector2 uv = new Vector2(position.x / sizeOfMap + 0.5f, position.y / sizeOfMap + 0.5f); // scale world to UVs
            var tempMat = new Material(_drawMaterial);
            tempMat.SetVector("_Center", uv);
            tempMat.SetFloat("_Size", _revealRadius / sizeOfMap);

            _commandBuffer.Blit(null, _currentlyRevealedArea, tempMat);
            _commandBuffer.SetRenderTarget(_fogTexture);
            _commandBuffer.Blit(_cameraRenderTexture, _fogTexture, tempMat);
        }

        Graphics.ExecuteCommandBuffer(_commandBuffer);
    }
}
