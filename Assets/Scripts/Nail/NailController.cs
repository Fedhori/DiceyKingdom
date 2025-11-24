using Data;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class NailController : MonoBehaviour
{
    public NailInstance Instance { get; private set; }

    bool initialized;

    public void Initialize(string nailId)
    {
        if (initialized)
        {
            Debug.LogWarning($"[NailController] Already initialized on {name}.");
            return;
        }

        NailDto dto;
        try
        {
            dto = NailRepository.GetOrThrow(nailId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NailController] Failed to initialize nail {nailId}: {e}");
            return;
        }

        Instance = new NailInstance(dto);
        initialized = true;
    }
}