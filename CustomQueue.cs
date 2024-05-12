using System.Collections;

namespace SharpMania;

public sealed class CustomQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
{
    public int Count => count;

    private T[] elements;
    private int front;
    private int rear;
    private int count;

    public CustomQueue(int capacity = 8)
    {
        elements = new T[capacity];
        front = 0;
        rear = -1;
        count = 0;
    }

    public T this[int index]
    {
        get 
        {
            if (!TryGet(index, out var result)) throw new IndexOutOfRangeException();
            return result;
        }
        set {
            if (!TrySet(index, value)) throw new IndexOutOfRangeException();
        }
    }

    public void Enqueue(T item)
    {
        if (IsFull())
        {
            Grow();
        }

        rear = (rear + 1) % elements.Length;
        elements[rear] = item;
        count++;
    }

    public T Dequeue()
    {
        if (IsEmpty())
        {
            Grow();
        }

        T item = elements[front];
        elements[front] = default!;
        front = (front + 1) % elements.Length;
        count--;
        return item;
    }

    public T Peek()
    {
        if (IsEmpty())
        {
            throw new InvalidOperationException("Queue is empty");
        }
        return elements[front];
    }

    public bool TryPeek(out T value)
    {
        if (IsEmpty()) 
        { 
            value = default!;
            return false;
        };
        value = elements[front];
        return true;
    }

    public bool TryGet(int orderIndex, out T value)
    {
        if (orderIndex < 0 || orderIndex >= count)
        {
            value = default!;
            return false;
        }
        value = elements[GetInternalIndexForElement(orderIndex)];
        return true;
    }

    public bool TrySet(int orderIndex, T value)
    {
        if (orderIndex < 0 || orderIndex >= count)
        {
            return false;
        }
        elements[GetInternalIndexForElement(orderIndex)] = value;
        return true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
        {
            yield return elements[GetInternalIndexForElement(i)];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    public bool IsEmpty() => count == 0;

    private bool IsFull() => count == elements.Length;

    private int GetInternalIndexForElement(int orderIndex)
    {
        return (front + orderIndex) % elements.Length;
    }

    private void Grow()
    {
        var newBuffer = new T[elements.Length * 2];
        if (rear > front)
        {
            Array.Copy(elements, front, newBuffer, 0, count);
        }
        else
        {
            Array.Copy(elements, front, newBuffer, 0, elements.Length - front);
            Array.Copy(elements, 0, newBuffer, elements.Length - front, rear + 1);
        }
        front = 0;
        rear = count - 1;
        elements = newBuffer;
    }
}
