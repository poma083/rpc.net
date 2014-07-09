using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDUDatas;
using System.Reflection;

namespace PDUServer
{
    public class InvokeMethodInfo
    {
        InvokeCfgElement config;
        Assembly assembly;
        Type instanceType;
        MethodInfo mi;
        object instance;
        bool isInstance;
        Type returnType;

        public InvokeMethodInfo(InvokeCfgElement cfg)
        {
            config = cfg;

            assembly = AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.FullName == cfg.Assembly).FirstOrDefault();
            instanceType = assembly.GetTypes().Where(t => t.FullName == cfg.InstanceType).FirstOrDefault();
            BindingFlags bf = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            mi = instanceType.GetMethod(cfg.Method, bf);
            returnType = mi.ReturnType;
            isInstance = !mi.IsStatic;
            if (isInstance)
            {
                instance = instanceType.GetConstructor(new Type[] { }).Invoke(new object[] { });
            }
        }
        public Assembly Assembly
        {
            get
            {
                return assembly;
            }
        }
        public Type InstanceType
        {
            get
            {
                return instanceType;
            }
        }
        public Type ReturnType
        {
            get
            {
                return returnType;
            }
        }
        public MethodInfo MethodInfo
        {
            get
            {
                return mi;
            }
        }
        public object Instance
        {
            get
            {
                return instance;
            }
        }
        public bool IsInstance
        {
            get
            {
                return isInstance;
            }
        }
        public string MethodName
        {
            get
            {
                return config.Method;
            }
        }
    }
    public class InvokeMethodsContainer
    {
        private Dictionary<string, InvokeMethodInfo> invokeMethods = new Dictionary<string, InvokeMethodInfo>();
        private static volatile InvokeMethodsContainer instance;
        public static object syncObject = new Object();

        private InvokeMethodsContainer()
        {
            PDUConfigSection sec = PDUConfigSection.GetConfig();
            foreach(InvokeCfgElement elem in sec.Server){
                invokeMethods.Add(elem.Name, new InvokeMethodInfo(elem));
            }
        }

        public static InvokeMethodsContainer Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncObject)
                    {
                        if (instance == null)
                            instance = new InvokeMethodsContainer();
                    }
                }
                return instance;
            }
        }

        public InvokeMethodInfo this[string name]
        {
            get
            {
                return invokeMethods[name];
            }
        }
    }
}
