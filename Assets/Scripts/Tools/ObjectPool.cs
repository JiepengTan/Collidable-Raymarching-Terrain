using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;



public static class ObjectPool<T> {
    private static Stack<T> pool = new Stack<T>();
    public static int Count {
        get {
            lock (pool) {
                return pool.Count;
            }
        }
    }
    public static void InitPool(int count) {
        for (int i = 0; i < count; i++) {
            T t = Get();
            Return(t);
        }
    }
    public static T Get() {
        if (Count > 0) {
            T obj;
            lock (pool) {
                obj = pool.Pop();
            }
            return obj;
        } else {
            return System.Activator.CreateInstance<T>();
        }

    }
    public static void Return(T obj) {
        lock (pool) {
            pool.Push(obj);
        }
    }
}


public interface IPoolElem {
    void OnInit(int hashCode);
    void OnRecycle();
    int HashVal { get; }
}


public static class DictionaryPool<T> where T : IPoolElem {
    [Serializable]
    public class IntComparer : IEqualityComparer<int> {
        bool IEqualityComparer<int>.Equals(int x, int y) { return x == y; }
        int IEqualityComparer<int>.GetHashCode(int obj) { return obj.GetHashCode(); }
    }

    public static readonly IntComparer _IntComparer = new IntComparer();

    private static Dictionary<int, Stack<T>> pools = new Dictionary<int, Stack<T>>(_IntComparer);

    private static T CreateInstance(int hashCode) {
        var ret = System.Activator.CreateInstance<T>();
        ret.OnInit(hashCode);
        return ret;
    }
    public static T Get(int hashCode) {
        lock (pools) {
            if (pools.ContainsKey(hashCode) && pools[hashCode].Count > 0) {
                return pools[hashCode].Pop();
            }
        }
        return CreateInstance(hashCode);
    }
    public static void Return(T elem) {
        lock (pools) {
            if (pools.ContainsKey(elem.HashVal) == false)
                pools.Add(elem.HashVal, new Stack<T>());
            if (pools[elem.HashVal].Count < (MapDataGen.CHUNK_SIZE / (elem.HashVal - 1)) * 5) {
                pools[elem.HashVal].Push(elem);
                elem.OnRecycle();
            }
        }
    }
}

