using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour {
    
    static ThreadedDataRequester instance;
    Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();
    
    void Awake() {
        instance = FindAnyObjectByType<ThreadedDataRequester>();
    }
    // When someone calls RequestData, they pass in the method they want to use the generate that data
    public static void RequestData(Func<object> generateData, Action<object> callback) { // object with small o = System.Object, not Unity object
        ThreadStart threadStart = delegate {
            instance.DataThread (generateData, callback);
        };

        new Thread(threadStart).Start();
    }

    void DataThread(Func<object> generateData, Action<object> callback) {
        object data = generateData();
        lock (dataQueue) { // When 1 thread reaches this point, whilst executing this, no other thread can execute it, and has to wait
          dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    void Update() {
        if (dataQueue.Count > 0) {
            for (int i = 0; i < dataQueue.Count; i++) {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    
    struct ThreadInfo {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
