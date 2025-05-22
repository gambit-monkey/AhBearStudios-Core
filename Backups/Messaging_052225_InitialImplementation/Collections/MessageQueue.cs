using System;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Collections
{
    public struct MessageQueue<T> : IDisposable where T : unmanaged
    {
        private MessageBuffer<T> _readBuffer;
        private MessageBuffer<T> _writeBuffer;
        private NativeReference<bool> _isSwapping;

        public MessageQueue(Allocator allocator)
        {
            _readBuffer = new MessageBuffer<T>(allocator);
            _writeBuffer = new MessageBuffer<T>(allocator);
            _isSwapping = new NativeReference<bool>(false, allocator);
        }

        public void Enqueue(T message)
        {
            while (_isSwapping.Value)
            {
                // Wait while buffers are being swapped
            }
            _writeBuffer.Add(message);
        }

        public NativeArray<T> SwapBuffers(out int count)
        {
            var isSwapping = _isSwapping;
            isSwapping.Value = true;

            // Clear the current read buffer
            var readBuffer = _readBuffer;
            readBuffer.Clear();
            _readBuffer = readBuffer;

            // Swap the buffers
            var temp = _readBuffer;
            _readBuffer = _writeBuffer;
            _writeBuffer = temp;

            isSwapping.Value = false;

            // Return messages from the new read buffer
            return _readBuffer.GetMessages(out count);
        }

        public void Dispose()
        {
            _readBuffer.Dispose();
            _writeBuffer.Dispose();
            if (_isSwapping.IsCreated)
            {
                _isSwapping.Dispose();
            }
        }
    }
}