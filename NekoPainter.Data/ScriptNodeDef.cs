using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Data
{
    public class ScriptNodeDef
    {
        public List<ScriptNodeIODef> ioDefs;
        public List<ScriptNodeParamDef> parameters;
        public string name;
        public string displayName;
        public string path;
        public string language;
        public bool hidden;
        public bool animated;
        public ScriptNodeIOFlag flags;
    }
    public class ScriptNodeIODef
    {
        public string ioType;
        public string name;
        public string displayName;
        public string type;

        public ScriptNodeIODef Clone()
        {
            ScriptNodeIODef scriptNodeIODef = (ScriptNodeIODef)MemberwiseClone();
            return scriptNodeIODef;
        }
    }
    public class ScriptNodeParamDef
    {
        public string name;
        public string displayName;
        public string description;
        public string type;
        public string defaultValue;
        public float step = 1.0f;
        [NonSerialized]
        public object defaultValue1;
    }
    [Flags]
    public enum ScriptNodeIOFlag
    {
        IsReadOnly = 1,
    }
}
