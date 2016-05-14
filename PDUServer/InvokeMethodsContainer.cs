using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDUDatas;
using System.Reflection;

namespace PDUServer
{
    internal class InvokeMethodInfo
    {
        InvokeCfgElement config;
        Assembly assembly;
        Type instanceType;
        MethodInfo mi;
        object instance;
        bool isInstance;
        Type returnType;

        internal InvokeMethodInfo(InvokeCfgElement cfg)
        {
            config = cfg;

            Assembly.Load(cfg.Assembly);
            Assembly[] asses = AppDomain.CurrentDomain.GetAssemblies();
            Type[] @params = null;
            if (cfg.Params.Count > 0)
            {
                @params = new Type[cfg.Params.Count];
                for(int i = 0; i< cfg.Params.Count; i++)
                {
                    InvokeParam param = cfg.Params[i];
                    if (String.IsNullOrEmpty(param.Assembly))
                    {
                        foreach (Assembly ass in asses)
                        {
                            Type[] types = ass.GetTypes();
                            foreach (Type type in types)
                            {
                                if (type.FullName.Equals(param.Type))
                                {
                                    @params[i] = type;
                                    break;
                                }
                            }
                            if (@params[i] != null)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        Assembly pTypeAss = Assembly.Load(param.Assembly);
                        @params[i] = pTypeAss.GetType(param.Type);
                    }
                }
            }

            assembly = asses.Where(ass => ass.FullName == cfg.Assembly).FirstOrDefault();
            instanceType = assembly.GetTypes().Where(t => t.FullName == cfg.InstanceType).FirstOrDefault();
            BindingFlags bf = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            if (@params == null)
            {
                mi = instanceType.GetMethod(cfg.Method, bf);
            }
            else
            {
                mi = instanceType.GetMethod(cfg.Method, bf, null, @params, null);
            }
            returnType = mi.ReturnType;
            isInstance = !mi.IsStatic;
            if (isInstance)
            {
                instance = instanceType.GetConstructor(new Type[] { }).Invoke(new object[] { });
            }
        }
        internal Assembly Assembly
        {
            get
            {
                return assembly;
            }
        }
        internal Type InstanceType
        {
            get
            {
                return instanceType;
            }
        }
        internal Type ReturnType
        {
            get
            {
                return returnType;
            }
        }
        internal MethodInfo MethodInfo
        {
            get
            {
                return mi;
            }
        }
        internal object Instance
        {
            get
            {
                return instance;
            }
        }
        internal bool IsInstance
        {
            get
            {
                return isInstance;
            }
        }
        internal string MethodName
        {
            get
            {
                return config.Method;
            }
        }
    }
    internal class InvokeMethodsContainer
    {
        Dictionary<string, InvokeMethodInfo> invokeMethods = new Dictionary<string, InvokeMethodInfo>();
        static volatile InvokeMethodsContainer instance;
        static object syncObject = new Object();

        private InvokeMethodsContainer()
        {
            Logger.Log.Info("Конфигурируем именованые вызовы процедур");
            PDUConfigSection sec = PDUConfigSection.GetConfig();
            foreach (InvokeCfgElement elem in sec.Server)
            {
                Logger.Log.InfoFormat("Процедура \"{0}\"", elem);
                invokeMethods.Add(elem.Name, new InvokeMethodInfo(elem));
            }
        }

        internal static InvokeMethodsContainer Instance
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

        internal InvokeMethodInfo this[string name]
        {
            get
            {
                if (invokeMethods.ContainsKey(name))
                {
                    return invokeMethods[name];
                }
                return null;
            }
        }
    }
}
