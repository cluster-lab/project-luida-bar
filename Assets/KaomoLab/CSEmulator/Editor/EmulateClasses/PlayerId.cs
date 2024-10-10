using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class PlayerId
    {
        public string id => playerHandle.id;
        public string __idfc => playerHandle.idfc;

        public PlayerHandle playerHandle { get; private set; }

        public PlayerId(
            PlayerHandle playerHandle
        )
        {
            this.playerHandle = playerHandle;
        }

        public object toJSON(string key)
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            o.id = id;
            return o;
        }
        public override string ToString()
        {
            return String.Format("[PlayerId][{0}]", id);
        }
    }
}
