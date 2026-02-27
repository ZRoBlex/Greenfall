using System;
using System.Collections.Generic;

/// <summary>
/// Min-Heap Priority Queue optimized for A* pathfinding
/// O(log n) insertion instead of O(n) with List
/// </summary>
public class PriorityQueue<T> where T : class
{
    private struct Element
    {
        public T item;
        public float priority;
    }

    private Element[] heap;
    private int count;
    private Dictionary<T, int> itemIndices; // For O(1) Contains check

    public int Count => count;

    public PriorityQueue(int capacity = 256)
    {
        heap = new Element[capacity];
        itemIndices = new Dictionary<T, int>(capacity);
        count = 0;
    }

    public void Enqueue(T item, float priority)
    {
        if (count == heap.Length)
        {
            Array.Resize(ref heap, heap.Length * 2);
        }

        heap[count] = new Element { item = item, priority = priority };
        itemIndices[item] = count;
        HeapifyUp(count);
        count++;
    }

    public T Dequeue()
    {
        if (count == 0)
            throw new InvalidOperationException("Queue is empty");

        Element min = heap[0];
        itemIndices.Remove(min.item);

        count--;
        if (count > 0)
        {
            heap[0] = heap[count];
            itemIndices[heap[0].item] = 0;
            HeapifyDown(0);
        }

        return min.item;
    }

    public bool Contains(T item)
    {
        return itemIndices.ContainsKey(item);
    }

    public void Clear()
    {
        count = 0;
        itemIndices.Clear();
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (heap[index].priority >= heap[parentIndex].priority)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int smallest = index;

            if (leftChild < count && heap[leftChild].priority < heap[smallest].priority)
                smallest = leftChild;

            if (rightChild < count && heap[rightChild].priority < heap[smallest].priority)
                smallest = rightChild;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int i, int j)
    {
        Element temp = heap[i];
        heap[i] = heap[j];
        heap[j] = temp;

        itemIndices[heap[i].item] = i;
        itemIndices[heap[j].item] = j;
    }
}

/// <summary>
/// Simple Vector2Int wrapper for priority queue
/// </summary>
public class PathNode
{
    public UnityEngine.Vector2Int position;
    public float gCost; // Distance from start
    public float hCost; // Heuristic to goal
    public float fCost => gCost + hCost;
    public PathNode parent;

    public void Set(UnityEngine.Vector2Int pos, float g, float h, PathNode par)
    {
        position = pos;
        gCost = g;
        hCost = h;
        parent = par;
    }
}
