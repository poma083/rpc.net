using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

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
                for (int i = 0; i < base.Count; i++)
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
        [ConfigurationProperty("password", IsKey = false, IsRequired = true)]
        public string Password { get { return this["password"] as string; } }
        [ConfigurationProperty("secureOnly", IsKey = false, IsRequired = true)]
        public bool SecureOnly { get { return (bool)this["secureOnly"]; } }

        [ConfigurationProperty("ServerCertificate")]
        public CertificateCfgElement ServerCertificate
        {
            get
            {
                return (CertificateCfgElement)this["ServerCertificate"] ?? new CertificateCfgElement();
            }
        }
    }
    [ConfigurationCollection(typeof(InvokeCfgElement), AddItemName = "InvokeInfo")]
    public class ClientCfgClass : ConfigurationElementCollection
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name { get { return this["name"] as string; } }
        [ConfigurationProperty("login", IsKey = false, IsRequired = true)]
        public string Login { get { return this["login"] as string; } }
        [ConfigurationProperty("password", IsKey = false, IsRequired = true)]
        public string Password { get { return this["password"] as string; } }
        [ConfigurationProperty("serverHost", IsKey = false, IsRequired = true)]
        public string Host { get { return this["serverHost"] as string; } }
        [ConfigurationProperty("serverPort", IsKey = false, IsRequired = true)]
        public UInt16 Port { get { return (UInt16)this["serverPort"]; } }
        [ConfigurationProperty("timeout", IsKey = false, IsRequired = true)]
        public UInt32 Timeout { get { return (UInt32)this["timeout"]; } }
        [ConfigurationProperty("genericNackPeriod", IsKey = false, IsRequired = true)]
        public UInt32 GenericNackPeriod { get { return (UInt32)this["genericNackPeriod"]; } }

        [ConfigurationProperty("ClientCertificate")]
        public CertificateCfgElement ClientCertificate
        {
            get
            {
                return (CertificateCfgElement)this["ClientCertificate"] ?? new CertificateCfgElement();
            }
        }
        [ConfigurationProperty("ServerCertificate")]
        public CertificateCfgElement ServerCertificate
        {
            get
            {
                return (CertificateCfgElement)this["ServerCertificate"] ?? new CertificateCfgElement();
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
    public class InvokeCfgElement : ConfigurationElement
    {
        [ConfigurationProperty("Params")]
        public InvokeParams Params
        {
            get
            {
                return (InvokeParams)this["Params"] ?? new InvokeParams();
            }
        }

        [ConfigurationProperty("name", IsRequired = false)]
        public string Name { get { return this["name"] as string; } }
        [ConfigurationProperty("assembly", IsRequired = false)]
        public string Assembly { get { return this["assembly"] as string; } }
        [ConfigurationProperty("instanceType", IsRequired = false)]
        public string InstanceType { get { return this["instanceType"] as string; } }
        [ConfigurationProperty("method", IsRequired = false)]
        public string Method { get { return this["method"] as string; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            PropertyInfo[] piList = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            sb.Append("{\"");
            sb.Append(this.GetType().Name);
            sb.Append("\":{");
            foreach (PropertyInfo pi in piList)
            {
                sb.Append("\"");
                sb.Append(pi.Name);
                sb.Append("\":\"");
                sb.Append(pi.GetValue(this, null));
                sb.Append("\",");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("}}");
            return sb.ToString();
        }
    }
    [ConfigurationCollection(typeof(InvokeParam), AddItemName = "Param")]
    public class InvokeParams : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new InvokeParam();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((InvokeParam)element).Id;
        }
        public InvokeParam this[int index]
        {
            get
            {
                return base.BaseGet(index) as InvokeParam;
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
    public class InvokeParam : ConfigurationElement
    {
        Guid id = Guid.NewGuid();
        public Guid Id { get { return id; } }
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type { get { return this["type"] as string; } }
        [ConfigurationProperty("assembly", IsRequired = true)]
        public string Assembly { get { return this["assembly"] as string; } }
    }

    public class CertificateCfgElement : ConfigurationElement
    {
        [ConfigurationProperty("thumbprint", IsRequired = true, IsKey = true)]
        public string Thumbprint { get { return this["thumbprint"] as string; } }
        [ConfigurationProperty("storeName", IsRequired = true)]
        public StoreName StoreName { get { return (StoreName)this["storeName"]; } }
        [ConfigurationProperty("storeLocation", IsRequired = true)]
        public StoreLocation StoreLocation { get { return (StoreLocation)this["storeLocation"]; } }
    }
}
