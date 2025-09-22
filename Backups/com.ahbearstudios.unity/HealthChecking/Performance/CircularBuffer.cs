using System;

namespace AhBearStudios.Unity.HealthChecking.Performance;

/// <summary>
/// Circular buffer for efficient performance sample storage
/// </summary>
public class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void Add(T item)
    {
        _buffer[_head] = item;
        _head = (_head + 1) % _buffer.Length;
        if (_count < _buffer.Length) _count++;
    }

    public int Count => _count;

    public float Average() where T : struct
    {
        if (_count == 0) return 0f;

        var sum = 0f;
        for (int i = 0; i < _count; i++)
        {
            sum += Convert.ToSingle(_buffer[i]);
        }
        return sum / _count;
    }
}