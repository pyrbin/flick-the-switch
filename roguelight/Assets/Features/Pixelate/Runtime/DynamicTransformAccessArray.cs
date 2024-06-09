using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;

namespace Ooze.Runtime.Pixelate.Runtime;

public struct DynamicTransformAccessArray : IDisposable
{
    private JobHandle _ReadPositionsJobHandle;
    private JobHandle _WritePositionsJobHandle;

    private NativeHashMap<int, int> _Id2Index;
    private NativeHashMap<int, int> _Index2Id;

    private NativeList<float3> _Positions;
    private NativeList<quaternion> _Rotations;
    private TransformAccessArray _Transforms;
    private bool _Initialized;

    private struct IdCounterKey {}
    private static readonly SharedStatic<int> IdCounter = SharedStatic<int>.GetOrCreate<int, IdCounterKey>();

    public readonly bool IsCreated => _Initialized;
    public readonly int Length => _Transforms.length;
    public readonly TransformAccessArray Transforms => _Transforms;
    public readonly NativeList<float3> Positions => _Positions;
    public readonly NativeList<quaternion> Rotations => _Rotations;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetLastIndex() => _Transforms.length - 1;

    public DynamicTransformAccessArray(int capacity, Allocator allocator)
    {
        _Positions = new(capacity, allocator);
        _Rotations = new(capacity, allocator);
        _Id2Index = new(capacity, allocator);
        _Index2Id = new(capacity, allocator);
        _Transforms = new(capacity);
        _ReadPositionsJobHandle = default;
        _WritePositionsJobHandle = default;
        _Initialized = true;
    }

    public void Dispose() => Dispose(default);

    public void Dispose(JobHandle dependency)
    {
        WaitTillJobsComplete();

        _Initialized = false;

        if (_Id2Index.IsCreated)
            _Id2Index.Dispose(dependency);
        if (_Index2Id.IsCreated)
            _Index2Id.Dispose(dependency);
        if (_Positions.IsCreated)
            _Positions.Dispose(dependency);
        if (_Rotations.IsCreated)
            _Rotations.Dispose(dependency);
        if (_Transforms.isCreated)
            _Transforms.Dispose();
    }

    public void WaitTillJobsComplete()
    {
        _ReadPositionsJobHandle.Complete();
        _WritePositionsJobHandle.Complete();
    }

    public int Register(Transform transform)
    {
        if (_Initialized == false) return 0;

        WaitTillJobsComplete();

        var id = CreateId();

        _Positions.Add(transform.position);
        _Rotations.Add(transform.rotation);
        _Transforms.Add(transform);

        return id;
    }

    private int CreateId()
    {
        var newId = ++IdCounter.Data;
        var index = GetLastIndex() + 1;
        _Id2Index[newId] = index;
        _Index2Id[index] = newId;
        return newId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetIndexId(int id)
    {
        if (_Id2Index.TryGetValue(id, out var index))
            return index;

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetIndexForId(int id, int index)
    {
#if UNITY_EDITOR
        Assert.IsTrue(GetIndexId(id) != -1); // Id we want to preserve, it will point to index
#endif
        _Id2Index[id] = index;
        _Index2Id[index] = id;
    }

    public (int index, int lastIndex) Deregister(int id)
    {
        if (_Initialized == false) return (0, 0);

        WaitTillJobsComplete();

        if (id == 0)
        {
#if UNITY_EDITOR
            Assert.IsTrue(false);
#endif
            return (0, 0);
        }

        var lastIndex = GetLastIndex();
        var index = GetIndexId(id);

#if UNITY_EDITOR
        Assert.IsTrue(lastIndex >= 0);
#endif

        if (index != lastIndex)
        {
            _Positions[index] = _Positions[lastIndex];
            _Rotations[index] = _Rotations[lastIndex];
            var idToSave = _Index2Id[lastIndex];
            SetIndexForId(idToSave, index);
        }

        _Id2Index.Remove(id);
        _Index2Id.Remove(lastIndex);
        _Positions.Length--;
        _Rotations.Length--;
        _Transforms.RemoveAtSwapBack(index);

        return (index, lastIndex);
    }

    [BurstCompile]
    internal struct GetPositionsJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<float3> _Positions;
        [WriteOnly] public NativeArray<quaternion> _Rotations;

        public void Execute(int index, TransformAccess transform)
        {
            _Positions[index] = transform.position;
            _Rotations[index] = transform.rotation;
        }
    }

    public JobHandle ScheduleReadTransforms(JobHandle dependency)
    {
        dependency = JobHandle.CombineDependencies(dependency, _ReadPositionsJobHandle, _WritePositionsJobHandle);

        _ReadPositionsJobHandle = new GetPositionsJob
        {
            _Positions = _Positions.AsArray(),
            _Rotations = _Rotations.AsArray(),
        }
        .ScheduleReadOnly(_Transforms, 256, dependency);

        return _ReadPositionsJobHandle;
    }

    public JobHandle ScheduleWriteTransforms<T>(T job, JobHandle dependency) where T : struct, IJobParallelForTransform
    {
        dependency = JobHandle.CombineDependencies(dependency, _ReadPositionsJobHandle, _WritePositionsJobHandle);
        _WritePositionsJobHandle = job.Schedule(_Transforms, dependency);
        return _WritePositionsJobHandle;
    }

    public JobHandle ScheduleReadWriteTransforms<T>(T job, JobHandle dependency) where T : struct, IJobParallelForTransform
    {
        ScheduleReadTransforms(dependency);

        dependency = JobHandle.CombineDependencies(dependency, _ReadPositionsJobHandle, _WritePositionsJobHandle);
        _WritePositionsJobHandle = job.Schedule(_Transforms, dependency);

        return _WritePositionsJobHandle;
    }
}
