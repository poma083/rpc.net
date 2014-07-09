using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PDUDatas
{
    public class PDUConfigSection : ConfigurationSection
    {
        public static PDUConfigSection GetConfig()
        {
            return (PDUConfigSection)ConfigurationManager.GetSection("PDUConfig") ?? new PDUConfigSection();
        }
        [ConfigurationProperty("Clients")]
        public ClientsCfgSection Clients
        {
            get
            {
                return (ClientsCfgSection)this["Clients"] ?? new ClientsCfgSection();
            }
        }
        [ConfigurationProperty("Server")]
        public ServerCfgClass Server
        {
            get
            {
                return (ServerCfgClass)this["Server"];// ?? new ServerCfgClass();
            }
        }
    }
    [ConfigurationCollection(typeof(ClientCfgClass), AddItemName = "Client")]
    public class ClientsCfgSection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ClientCfgClass();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ClientCfgClass)element).Name;
        }
        public ClientCfgClass this[string index]
        {
            get
            {
                for (int i = 0; i < base.Count; i++)
                {
                    if (((ClientCfgClass)base.BaseGet(i)).Name == index)
                    {
                        return (ClientCfgClass)base.BaseGet(i);
                    }
                }
                return null;
            }
        }
    }
    [ConfigurationCollection(typeof(InvokeCfgElement), AddItemName = "InvokeInfo")]
    public class ServerCfgClass : ConfigurationElementCollection
    {
        [ConfigurationProperty("bindHost", IsKey = true, IsRequired = true)]
        public string Host { get { return this["bindHost"] as string; } }
        [ConfigurationProperty("bindPort", IsKey = true, IsRequired = true)]
        public UInt16 Port { get { return (UInt16)this["bindPort"]; } }
        [ConfigurationProperty("enquireLinkPeriod", IsKey = true, IsRequired = true)]
        public UInt32 EnquireLinkPeriod { get { return (UInt32)this["enquireLinkPeriod"]; } }
        

        [ConfigurationProperty("Users")]
        public UsersCfgSection Users
        {
            get
            {
                return (UsersCfgSection)this["Users"] ?? new UsersCfgSection();
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new InvokeCfgElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((InvokeCfgElement)element).Name;
        }
        public InvokeCfgElement this[int index]
        {
            get
            {
                return base.BaseGet(index) as InvokeCfgElement;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
    }
    [ConfigurationCollection(typeof(UserCfgClass), AddItemName = "User")]
    public class UsersCfgSection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new UserCfgClass();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UserCfgClass)element).Login;
        }
        public UserCfgClass this[string index]
        {
            get
            {
                for (int i = 0; i < base.Count; i++ )
                {
                    if (((UserCfgClass)base.BaseGet(i)).Login == index)
                    {
                        return (UserCfgClass)base.BaseGet(i);
                    }
                }
                return null;
            }
        }
    }
    public class UserCfgClass : ConfigurationElement
    {
        [ConfigurationProperty("login", IsKey = true, IsRequired = true)]
        public string Login { get { return this["login"] as string; } }
        [ConfigurationProperty("password", IsKey = true, IsRequired = true)]
        public string Password { get { return this["password"] as string; } }
    }
    [ConfigurationCollection(typeof(InvokeCfgElement), AddItemName = "InvokeInfo")]
    public class ClientCfgClass : ConfigurationElementCollection
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name { get { return this["name"] as string; } }
        [ConfigurationProperty("login", IsKey = true, IsRequired = true)]
        public string Login { get { return this["login"] as string; } }
        [ConfigurationProperty("password", IsKey = true, IsRequired = true)]
        public string Password { get { return this["password"] as string; } }
        [ConfigurationProperty("serverHost", IsKey = true, IsRequired = true)]
        public string Host { get { return this["serverHost"] as string; } }
        [ConfigurationProperty("serverPort", IsKey = true, IsRequired = true)]
        public UInt16 Port { get { return (UInt16)this["serverPort"]; } }
        [ConfigurationProperty("timeout", IsKey = true, IsRequired = true)]
        public UInt32 Timeout { get { return (UInt32)this["timeout"]; } }
        [ConfigurationProperty("genericNackPeriod", IsKey = true, IsRequired = true)]
        public UInt32 GenericNackPeriod { get { return (UInt32)this["genericNackPeriod"]; } }

        protected override ConfigurationElement CreateNewElement()
        {
            return new InvokeCfgElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((InvokeCfgElement)element).Name;
        }
        public InvokeCfgElement this[int index]
        {
            get
            {
                return base.BaseGet(index) as InvokeCfgElement;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
    }

    public class InvokeCfgElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = false)]
        public string Name { get { return this["name"] as string; } }
        [ConfigurationProperty("assembly", IsRequired = false)]
        public string Assembly { get { return this["assembly"] as string; } }
        [ConfigurationProperty("instanceType", IsRequired = false)]
        public string InstanceType { get { return this["instanceType"] as string; } }
        [ConfigurationProperty("method", IsRequired = false)]
        public string Method { get { return this["method"] as string; } }
    }
}
