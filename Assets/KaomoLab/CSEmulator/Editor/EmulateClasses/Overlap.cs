using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class Overlap
    {
        public HitObject @object { get; private set; }
        public object selfNode { get; private set; }

        public Overlap(
            HitObject hitObject,
            object selfNode
        )
        {
            this.@object = hitObject;
            this.selfNode = selfNode;
        }

        public object toJSON(string key)
        {
            return this;
        }
        public override string ToString()
        {
            return String.Format("[Overlap][{0}][{1}]", @object, selfNode);
        }
    }
}
