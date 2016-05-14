using PDUDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PDUServer
{
    internal class InvokeEvent
    {
        AutoResetEvent autoEvent;
        ManualResetEvent manualEvent;

        internal InvokeEvent(string name)
        {
            autoEvent = new AutoResetEvent(false);
            manualEvent = new ManualResetEvent(false);
        }
        internal void Set()
        {
            autoEvent.Set();
            manualEvent.Set();
        }
        internal void Reset()
        {
            manualEvent.Reset();
        }
        internal void WaitAuto()
        {
            autoEvent.WaitOne();
        }
        internal void WaitManual()
        {
            manualEvent.WaitOne();
        }
    }
    internal class InvokeEventsContainer
    {
        Dictionary<string, InvokeEvent> invokeEvents = new Dictionary<string, InvokeEvent>();
        static volatile InvokeEventsContainer instance;
        static object syncObject = new Object();

        private InvokeEventsContainer()
        {
        }

        internal static InvokeEventsContainer Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncObject)
                    {
                        if (instance == null)
                            instance = new InvokeEventsContainer();
                    }
                }
                return instance;
            }
        }

        internal InvokeEvent Create(MethodInfo methodInfo, Type[] argumentTypes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(methodInfo.ReflectedType.FullName);
            sb.Append('.');
            sb.Append(methodInfo.Name);
            sb.Append('(');
            if (argumentTypes.Length > 0)
            {
                foreach (Type t in argumentTypes)
                {
                    sb.Append(t.Name);
                    sb.Append(',');
                }
                sb.Remove(sb.Length - 2, 1);
            }
            sb.Append(')');
            return Create(sb.ToString());
        }
        internal InvokeEvent Create(string key)
        {
            InvokeEvent result = null;
            lock (syncObject)
            {
                invokeEvents.TryGetValue(key, out result);
                if (result == null)
                {
                    result = new InvokeEvent(key);
                    invokeEvents.Add(key, result);
                }
            }
            return result;
        }
        internal InvokeEvent Set(MethodInfo methodInfo, Type[] argumentTypes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(methodInfo.ReflectedType.FullName);
            sb.Append('.');
            sb.Append(methodInfo.Name);
            sb.Append('(');
            if (argumentTypes.Length > 0)
            {
                foreach (Type t in argumentTypes)
                {
                    sb.Append(t.Name);
                    sb.Append(',');
                }
                sb.Remove(sb.Length - 2, 1);
            }
            sb.Append(')');
            return Set(sb.ToString());
        }
        internal InvokeEvent Set(string waitName)
        {
            InvokeEvent result = null;
            lock (syncObject)
            {
                invokeEvents.TryGetValue(waitName, out result);
                if (result == null)
                {
                    result = new InvokeEvent(waitName);
                    invokeEvents.Add(waitName, result);
                }
                result.Set();
                result.Reset();
            }
            return result;
        }
        internal InvokeEvent Reset(MethodInfo methodInfo, Type[] argumentTypes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(methodInfo.ReflectedType.FullName);
            sb.Append('.');
            sb.Append(methodInfo.Name);
            sb.Append('(');
            if (argumentTypes.Length > 0)
            {
                foreach (Type t in argumentTypes)
                {
                    sb.Append(t.Name);
                    sb.Append(',');
                }
                sb.Remove(sb.Length - 2, 1);
            }
            sb.Append(')');
             return Reset(sb.ToString());
        }
        internal InvokeEvent Reset(string key)
        {
            InvokeEvent result;
            lock (syncObject)
            {
                invokeEvents.TryGetValue(key, out result);
                if (result == null)
                {
                    result = new InvokeEvent(key);
                    invokeEvents.Add(key, result);
                }
                result.Reset();
            }
            return result;
        }
        //internal void WaitAuto(MethodInfo methodInfo, Type[] argumentTypes)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(methodInfo.ReflectedType.FullName);
        //    sb.Append('.');
        //    sb.Append(methodInfo.Name);
        //    sb.Append('(');
        //    if (argumentTypes.Length > 0)
        //    {
        //        foreach (Type t in argumentTypes)
        //        {
        //            sb.Append(t.Name);
        //            sb.Append(',');
        //        }
        //        sb.Remove(sb.Length - 2, 1);
        //    }
        //    sb.Append(')');
        //    WaitAuto(sb.ToString());
        //}
        //internal void WaitAuto(string key)
        //{
        //    lock (syncObject)
        //    {
        //        InvokeEvent @event;
        //        invokeEvents.TryGetValue(key, out @event);
        //        if (@event != null)
        //        {
        //            @event.WaitAuto();
        //        }
        //    }
        //}
        //internal void WaitManual(MethodInfo methodInfo, Type[] argumentTypes)
        //{
        //    ParameterInfo[] p = methodInfo.GetParameters();
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(methodInfo.ReflectedType.FullName);
        //    sb.Append('.');
        //    sb.Append(methodInfo.Name);
        //    sb.Append('(');
        //    if (argumentTypes.Length > 0)
        //    {
        //        foreach (Type t in argumentTypes)
        //        {
        //            sb.Append(t.Name);
        //            sb.Append(',');
        //        }
        //        sb.Remove(sb.Length - 2, 1);
        //    }
        //    sb.Append(')');
        //    WaitManual(sb.ToString());
        //}
        //internal void WaitManual(string key)
        //{
        //    lock (syncObject)
        //    {
        //        InvokeEvent @event;
        //        invokeEvents.TryGetValue(key, out @event);
        //        if (@event != null)
        //        {
        //            @event.WaitManual();
        //        }
        //    }
        //}
    }
}
