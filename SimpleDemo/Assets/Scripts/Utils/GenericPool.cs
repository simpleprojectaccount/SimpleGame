using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class GenericPool<T> where T : class
{
    private Stack<T> _pool = null;
    public Func<T> CreateFunction { get; set; }
    public Action<T> OnPush { get; set; }
    public Action<T> OnPop { get; set; }

    public GenericPool()
    {
        _pool = new Stack<T>();
    }

    public bool Populate(int count)
    {
        if (count <= 0)
            return true;

        // Create a single object first to see if everything works fine
        // If not, return false
        T obj = New();
        if (obj == null)
            return false;

        Push(obj);

        // Everything works fine, populate the pool with the remaining items
        for (int i = 1; i < count; i++)
            Push(New());

        return true;
    }

    // Fetch an item from the pool
    public T Pop()
    {
        T objToPop;

        if (_pool.Count == 0)
        {
            // Pool is empty, create new object
            objToPop = New();
        }
        else
        {
            // Pool is not empty, fetch the first item in the pool
            objToPop = _pool.Pop();
            while (objToPop == null)
            {
                // Some objects in the pool might have been destroyed (maybe during a scene transition),
                // consider that case
                if (_pool.Count > 0)
                    objToPop = _pool.Pop();
                else
                {
                    objToPop = New();
                    break;
                }
            }
        }

        OnPop?.Invoke(objToPop);

        return objToPop;
    }

    // Pool an item
    public void Push(T obj)
    {
        if (obj == null) return;
        OnPush?.Invoke(obj);
        _pool.Push(obj);
    }

    // Create a new object
    private T New()
    {
        if (CreateFunction != null)
            return CreateFunction();
        return null;
    }
}