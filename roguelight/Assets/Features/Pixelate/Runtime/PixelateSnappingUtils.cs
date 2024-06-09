using UnityEngine;
using Unity.Collections;
using UnityEngine.Jobs;
using System.Runtime.CompilerServices;
using Unity.Burst;

namespace Ooze.Runtime.Pixelate.Runtime;

public static class PixelateSnappingUtils {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 RoundToPixel(float unitsPerPixel, float3 position) {
        if (unitsPerPixel == 0.0f) return position;
        return math.round(position / unitsPerPixel) * unitsPerPixel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PixelSnap(quaternion cameraRotation, float unitsPerPixel, Transform target, out float3 displacement)
    {
        float3 alignedPos = math.mul(math.inverse(cameraRotation), target.position);
        float3 snappedPos = RoundToPixel(unitsPerPixel, alignedPos);
        displacement = alignedPos - snappedPos;
        target.position = math.mul(cameraRotation, snappedPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PixelSnap(quaternion cameraRotation, float unitsPerPixel, TransformAccess target)
    {
        float3 alignedPos = math.mul(math.inverse(cameraRotation), target.position);
        float3 snappedPos = RoundToPixel(unitsPerPixel, alignedPos);
        target.position = math.mul(cameraRotation, snappedPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PixelSnapAngle( int resolution, Transform target, out float3 displacement) {
        var angles = target.rotation.eulerAngles;
        var snapped = RoundToPixel(resolution, angles);
        target.rotation = quaternion.Euler(snapped);
        displacement = (float3)angles - snapped;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PixelSnapAngle(int resolution, TransformAccess target) {
        var angles = target.rotation.eulerAngles;
        var snapped = RoundToPixel(resolution, angles);
        target.rotation = quaternion.Euler(snapped);
    }

    [BurstCompile]
    internal struct SnapToPixelJob : IJobParallelForTransform
    {
        public quaternion CameraRotation;
        public float UnitsPerPixel;

        public void Execute(int index, TransformAccess transform)
        {
            PixelSnap(CameraRotation, UnitsPerPixel, transform);
        }
    }

    [BurstCompile]
    internal struct UnsnapFromPixelJob : IJobParallelForTransform
    {
        [Unity.Collections.ReadOnly] public NativeArray<float3> Positions;
        public void Execute(int index, TransformAccess transform)
        {
            transform.position = Positions[index];
        }
    }
}


