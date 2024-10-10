using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class RaycastResult
    {
        public readonly Hit hit;
        //@を付ければOK。CS側からは.objectで参照できる。propertyでも同じくOK。
        public readonly HitObject @object;

        public RaycastResult(
            Hit hit,
            HitObject hitObject
        )
        {
            this.hit = hit;
            this.@object = hitObject;
        }

        public object toJSON(string key)
        {
            return this;
        }
        public override string ToString()
        {
            return String.Format("[RaycastResult][{0}][{1}]", @object, hit);
        }
    }
}
