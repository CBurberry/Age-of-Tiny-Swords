using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class RevealEntry
{
    public Vector3 Pos;
    public float Radius;
}

public class FogOfWarManager : MonoBehaviour
{
    [SerializeField] GameObject _container;
    [SerializeField] RenderTexture _cameraRenderTexture;
    [SerializeField] RenderTexture _fogTexture;
    [SerializeField] RenderTexture _currentlyRevealedArea;
    [SerializeField] Material _drawMaterial;
    [SerializeField] float _sizeOfMap = 100f;

    Player _player;
    CommandBuffer _commandBuffer;

    Camera _camera;

    void Awake()
    {
        _camera = Camera.main;
        _commandBuffer = new CommandBuffer();
        _container.gameObject.SetActive(true);
        RenderTexture.active = _fogTexture;
        GL.Clear(true, true, Color.black);
        
        RenderTexture.active = _currentlyRevealedArea;
        GL.Clear(true, true, Color.black); 
        
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
    }

    private void Start()
    {
        _player = GameManager.GetPlayer(GameManager.Instance.CurrentPlayerFaction);
    }

    void LateUpdate()
    {
        List<RevealEntry> revealPositions = _player.Buildings.Select(x => new RevealEntry
        {
            Pos = x.transform.position + x.ColliderOffset,
            Radius = x.FOV
        }).ToList();

        revealPositions.AddRange(_player.Units.Select(x => new RevealEntry
        {
            Pos = x.transform.position + x.ColliderOffset,
            Radius = x.FOV
        }).ToList());
        RevealPositions(revealPositions);
    }

    void RevealPositions(List<RevealEntry> revealData)
    {
        _commandBuffer.Clear();
        _commandBuffer.SetRenderTarget(_currentlyRevealedArea);
        _commandBuffer.ClearRenderTarget(true, true, Color.black);

        for (int i = 0; i < revealData.Count; i++)
        { 
            var position = revealData[i].Pos;
            var radius = revealData[i].Radius;

            Vector2 uv = new Vector2(position.x / _sizeOfMap + 0.5f, position.y / _sizeOfMap + 0.5f); // scale world to UVs
            var tempMat = new Material(_drawMaterial); 
            tempMat.SetVector("_Center", uv);
            tempMat.SetFloat("_Size", radius / _sizeOfMap);
            _commandBuffer.Blit(null, _currentlyRevealedArea, tempMat);
            _commandBuffer.SetRenderTarget(_fogTexture);
            _commandBuffer.Blit(_cameraRenderTexture, _fogTexture, tempMat);
        }
        Graphics.ExecuteCommandBuffer(_commandBuffer);
    }

    public async UniTask UpdateArea(Vector3 position, float radius)
    {
        await UniTask.DelayFrame(2);

        Vector2 uv = new Vector2(position.x / _sizeOfMap + 0.5f, position.y / _sizeOfMap + 0.5f); // scale world to UVs
        var tempMat = new Material(_drawMaterial);
        tempMat.SetVector("_Center", uv);
        tempMat.SetFloat("_Size", radius / _sizeOfMap);
        _commandBuffer.SetRenderTarget(_fogTexture);
        _commandBuffer.Blit(_cameraRenderTexture, _fogTexture, tempMat);
        Graphics.ExecuteCommandBuffer(_commandBuffer);
    }
}
