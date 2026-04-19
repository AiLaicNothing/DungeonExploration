using UnityEngine;

public class PlatformRider : MonoBehaviour
{
    private MovingPlatform _current;

    public Vector3 CurrentPlatformVelocity =>
        _current != null ? _current.CurrentVelocity : Vector3.zero;

    public bool IsOnPlatform => _current != null;

    public void SetPlatform(MovingPlatform platform)
    {
        _current = platform;
    }

    public void ClearPlatform(MovingPlatform platform)
    {
        if (_current == platform) _current = null;
    }
}