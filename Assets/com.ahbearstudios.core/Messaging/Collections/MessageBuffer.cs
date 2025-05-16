using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Messaging.Collections
{
    public struct MessageBuffer<T> : IDisposable where T : unmanaged
    {
        private NativeList<T> _messages;
        private NativeReference<int> _count;
        private readonly Allocator _allocator;
        private readonly AtomicSafetyHandle _safetyHandle;

        public MessageBuffer(Allocator allocator)
        {
            _allocator = allocator;
            _messages = new NativeList<T>(allocator);
            _count = new NativeReference<int>(allocator);
            _safetyHandle = AtomicSafetyHandle.Create();
        }

        public void Add(T message)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            _messages.Add(message);
            var count = _count;
            count.Value++;
            _count = count;
        }

        public NativeArray<T> GetMessages(out int count)
        {
            AtomicSafetyHandle.CheckReadAndThrow(_safetyHandle);
            count = _count.Value;
            return _messages.AsArray();
        }

        public void Clear()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(_safetyHandle);
            _messages.Clear();
            var count = _count;
            count.Value = 0;
            _count = count;
        }

        public void Dispose()
        {
            if (_messages.IsCreated)
            {
                _messages.Dispose();
            }
            if (_count.IsCreated)
            {
                _count.Dispose();
            }
            AtomicSafetyHandle.Release(_safetyHandle);
        }
    }
}