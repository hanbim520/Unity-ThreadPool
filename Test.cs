using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Text;
using Amib.Threading;

public class Test : MonoBehaviour {
    private SmartThreadPool _smartThreadPool;
    private IWorkItemsGroup _workItemsGroup;

    private Func<long> _getActiveThreads;
    private Func<long> _getInUseThreads;
    private Func<long> _getQueuedWorkItems;
    private Func<long> _getCompletedWorkItems;
    private Thread workItemsProducerThread;
    private static bool _useWindowsPerformanceCounters;
    private long workItemsGenerated;
    private long workItemsCompleted;
    private bool running;
    // Use this for initialization
    void Start () {
        running = true;
        
        STPStartInfo stpStartInfo = new STPStartInfo();
        stpStartInfo.IdleTimeout = 50 * 1000;
        stpStartInfo.MaxWorkerThreads = 10;
        stpStartInfo.MinWorkerThreads = 2;
        if (_useWindowsPerformanceCounters)
        {
            stpStartInfo.PerformanceCounterInstanceName = "Test SmartThreadPool";
        }
        else
        {
            stpStartInfo.EnableLocalPerformanceCounters = true;
        }
        _smartThreadPool = new SmartThreadPool(stpStartInfo);
        _workItemsGroup = _smartThreadPool;

        workItemsProducerThread = new Thread(new ThreadStart(this.WorkItemsProducer));
        workItemsProducerThread.IsBackground = true;
        workItemsProducerThread.Start();
        InitializeLocalPerformanceCounters();

    }
    private object DoWork(object obj)
    {
        Thread.Sleep(100);
        Interlocked.Increment(ref workItemsCompleted);
        return null;
    }

    private void WorkItemsProducer()
    {
        WorkItemCallback workItemCallback = new WorkItemCallback(this.DoWork);
        while (running)
        {
            IWorkItemsGroup workItemsGroup = _workItemsGroup;
            if (null == workItemsGroup)
            {
                return;
            }

            try
            {
                workItemCallback = new WorkItemCallback(this.DoWork);
                workItemsGroup.QueueWorkItem(workItemCallback);
            }
            catch (System.ObjectDisposedException e)
            {
                e.GetHashCode();
                break;
            }
            workItemsGenerated++;
            Thread.Sleep(10);
        }
    }

    private void InitializeLocalPerformanceCounters()
    {
        _getActiveThreads = () => _smartThreadPool.PerformanceCountersReader.ActiveThreads;
        _getInUseThreads = () => _smartThreadPool.PerformanceCountersReader.InUseThreads;
        _getQueuedWorkItems = () => _smartThreadPool.PerformanceCountersReader.WorkItemsQueued;
        _getCompletedWorkItems = () => _smartThreadPool.PerformanceCountersReader.WorkItemsProcessed;

//         STPStartInfo stpStartInfo = new STPStartInfo();
//         stpStartInfo.PerformanceCounterInstanceName = "Test SmartThreadPool";
// 
//         SmartThreadPool stp = new SmartThreadPool(stpStartInfo);
//         stp.Shutdown();
    }
    // Update is called once per frame
    void Update () {
        SmartThreadPool stp = _smartThreadPool;
        if (null == stp)
        {
            return;
        }

        //int threadsInUse = (int)_pcInUseThreads.NextValue();
        //int threadsInPool = (int)_pcActiveThreads.NextValue();
        int threadsInUse = (int)_getInUseThreads();
        int threadsInPool = (int)_getActiveThreads();

        Debug.Log("threadsInUse=>" + threadsInUse.ToString());
        Debug.Log("threadsInPool=>" + threadsInPool.ToString());
        Debug.Log(_getQueuedWorkItems().ToString());
        Debug.Log(threadsInUse);
        Debug.Log(threadsInPool);
        Debug.Log(_getCompletedWorkItems().ToString());
    }
}
