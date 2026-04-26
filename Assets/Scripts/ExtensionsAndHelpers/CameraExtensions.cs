using UnityEngine;

public static class CameraExtensions
{
    public static float GetHorizontalFieldOfView(this Camera camera)
        => 2f * Mathf.Atan(Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * camera.aspect) * Mathf.Rad2Deg;
}
