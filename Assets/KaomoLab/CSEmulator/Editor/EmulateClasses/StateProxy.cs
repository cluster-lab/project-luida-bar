using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class StateProxy
        : Components.IVariablesStore
    {
        readonly string pauseFrameIndex;
        readonly ISendableSanitizer sendableSanitizer;
        readonly IRunningContext runningContext;
        readonly IJsValueConverter jsValueConverter;

        readonly Dictionary<string, object> state;

        public StateProxy(
            string pauseFrameIndex,
            IRunningContext runningContext,
            ISendableSanitizer sendableSanitizer,
            IJsValueConverter jsValueConverter
        )
        {
            this.pauseFrameIndex = pauseFrameIndex;
            this.sendableSanitizer = sendableSanitizer;
            this.runningContext = runningContext;
            this.jsValueConverter = jsValueConverter;
            state = new Dictionary<string, object>();
        }

        public Jint.Native.JsValue this[string index]
        {
            get
            {
                if (runningContext.CheckTopLevel("ClusterScript.State")) { }; //メッセージのみ
                if (!state.ContainsKey(index))
                    return Jint.Native.JsValue.Undefined;

                var obj = state[index];

                obj = sendableSanitizer.Sanitize(obj);

                var ret = jsValueConverter.FromObject(obj);

                return ret;
            }
            set
            {
                if(index == pauseFrameIndex) UnityEngine.Debug.Break();
                if (runningContext.CheckTopLevel("ClusterScript.State")) { }; //メッセージのみ
                if (value is Jint.Native.JsUndefined) return;
                state[index] = sendableSanitizer.Sanitize(value);
                _OnVariablesUpdated.Invoke();
            }
        }

        public static int CalcSendableSize(
            object value, int arrayAddition, int size = 0
        )
        {
            var add = 0;
            if (value == null)
            {
                add = 0;
            }
            else if (value is Jint.Native.JsValue jsv && jsv == Jint.Native.JsValue.Undefined)
            {
                throw new Exception("undefinedはSendableではありません。nullを指定してください。");
            }
            else if (value is bool boolValue)
            {
                add = 2;
            }
            else if (value is int intValue)
            {
                add = 9; //たぶん
            }
            else if (value is float floatValue)
            {
                add = 9; //たぶん
            }
            else if (value is double doubleValue)
            {
                add = 9; //数字は基本これで入ってくる
            }
            else if (value is string stringValue)
            {
                //"a":1、"あ":3
                var count = Encoding.UTF8.GetByteCount(stringValue);
                add = 2 + count;
            }
            else if (value.GetType().IsArray)
            {
                var objects = (object[])value;
                add = 2 + objects.Select(o => CalcSendableSize(o, 2, size)).Sum();
            }
            else if (value is ISendableSize sendableSize)
            {
                add = sendableSize.GetSize();
            }
            else if (value is System.Dynamic.ExpandoObject eo)
            {
                add = 2;
                foreach (var kv in eo.ToArray())
                {
                    add += Encoding.UTF8.GetByteCount(kv.Key);
                    add += 6 + CalcSendableSize(kv.Value, 0, size);
                }
            }

            //階層が深くなると加算される
            add += arrayAddition;

            //なんだか分からないけど130に入った瞬間に+1される
            if (size < 130 && size + add >= 130) add++;

            return size + add;
        }

        public object toJSON(string key)
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            return o;
        }
        public override string ToString()
        {
            return String.Format("[StateProxy]");
        }


        event Action _OnVariablesUpdated = delegate { };
        event Action Components.IVariablesStore.OnVariablesUpdated
        {
            add => _OnVariablesUpdated += value;
            remove => _OnVariablesUpdated -= value;
        }

        public class JsValueWrapper
            : Components.IVariable
        {
            public string name { get; private set; }
            public string value { get; private set; }
            public string type { get; private set; }
            public bool hasChild { get; private set; }
            public IEnumerable<Components.IVariable> children { get; private set; }

            readonly PropertyInfo prop_target;

            readonly Jint.Native.JsValue jsValue;
            readonly ISendableSanitizer sendableSanitizer;
            readonly IJsValueConverter jsValueConverter;
            public JsValueWrapper(
                string name,
                Jint.Native.JsValue jsValue,
                ISendableSanitizer sendableSanitizer,
                IJsValueConverter jsValueConverter
            )
            {
                this.sendableSanitizer = sendableSanitizer;
                this.jsValueConverter = jsValueConverter;
                this.name = name;
                this.value = jsValue.ToString();
                this.type = jsValue.Type.ToString();
                this.jsValue = jsValue;
                prop_target = jsValue.GetType().GetProperty("Target");
                if (jsValue.IsArray())
                {
                    //ObjectではなくArrayを取る方法がないため
                    this.type = "Array";
                    this.children = GetArrayChildren();
                    this.hasChild = true;
                }
                else if(jsValue.Type == Jint.Runtime.Types.Object)
                {
                    this.children = GetObjectChildren();
                    this.type = GetObjectTypeName();
                    this.hasChild = true;
                }
                else
                {
                    this.hasChild = false;
                }
            }
            string GetObjectTypeName()
            {
                if(prop_target == null)
                {
                    return jsValue.Type.ToString();
                }
                var targetType = prop_target.GetValue(jsValue);
                if (targetType is IHasTypeNameAlias alias)
                {
                    return alias.GetAliasTypeName();
                }
                return targetType.GetType().Name;
            }
            IEnumerable<Components.IVariable> GetArrayChildren()
            {
                if (jsValue is Jint.Native.Array.ArrayInstance ai)
                {
                    var index = 0;
                    foreach (var o in ai)
                    {
                        var sanitized = sendableSanitizer.Sanitize(o);
                        var jv = jsValueConverter.FromObject(sanitized);
                        var ret = new JsValueWrapper(index.ToString(), jv, sendableSanitizer, jsValueConverter);
                        index++;
                        yield return ret;
                    }
                }
                yield break;
            }
            IEnumerable<Components.IVariable> GetObjectChildren()
            {
                if (jsValue is Jint.Native.Object.ObjectInstance oi)
                {
                    foreach (var key in oi.GetOwnPropertyKeys(Jint.Runtime.Types.String))
                    {
                        var sanitized = sendableSanitizer.Sanitize(oi[key]);
                        var target = prop_target?.GetValue(jsValue);
                        if (target != null && (target is IHasUnofficialMembers unofficialMembers))
                        {
                            if (unofficialMembers.GetPropertyNames().Contains(key.ToString())) continue;
                        }
                        var jv = jsValueConverter.FromObject(sanitized);
                        var ret = new JsValueWrapper(key.ToString(), jv, sendableSanitizer, jsValueConverter);
                        yield return ret;
                    }

                }
                yield break;
            }

        }
        IEnumerable<Components.IVariable> Components.IVariablesStore.GetVariables()
        {
            foreach(var key in state.Keys){
                var ret = new JsValueWrapper(key, this[key], sendableSanitizer, jsValueConverter);
                yield return ret;
            }
        }
    }
}
