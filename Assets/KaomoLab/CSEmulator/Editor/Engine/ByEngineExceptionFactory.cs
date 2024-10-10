using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class ByEngineExceptionFactory
        : IItemExceptionFactory
    {
        public readonly Jint.Native.Error.ErrorConstructor clusterScriptErrorConstructor;

        readonly Jint.Engine engine;

        public ByEngineExceptionFactory(
            Jint.Engine engine
        )
        {
            this.engine = engine;
            clusterScriptErrorConstructor = CreateErrorConstructor(
                nameof(ClusterScriptError),
                engine,
                GetPrototypeObject(engine.Realm.Intrinsics.Error),
                intrinsics => GetPrototypeObject(clusterScriptErrorConstructor)
            );
        }

        Jint.Native.Object.ObjectInstance GetPrototypeObject(Jint.Native.Error.ErrorConstructor source)
        {
            var property = typeof(Jint.Native.Error.ErrorConstructor)
                .GetProperty("PrototypeObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(source);
            return (Jint.Native.Object.ObjectInstance)property;
        }
        Jint.Native.Error.ErrorConstructor CreateErrorConstructor(
            string name,
            Jint.Engine engine,
            Jint.Native.Object.ObjectInstance objectPrototype,
            Func<Jint.Runtime.Intrinsics, Jint.Native.Object.ObjectInstance> intrinsicDefaultProto
        ){
            var ret = Activator.CreateInstance(
                typeof(Jint.Native.Error.ErrorConstructor),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[]
                {
                    engine,
                    engine.Realm,
                    engine.Realm.Intrinsics.Error,
                    objectPrototype,
                    new Jint.Native.JsString(name),
                    intrinsicDefaultProto
                },
                null
            );
            return (Jint.Native.Error.ErrorConstructor)ret;
        }

        public Exception CreateDistanceLimitExceeded(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, "[DistanceLimitExceeded]" + message)
            {
                distanceLimitExceeded = true
            };
        }

        public Exception CreateRateLimitExceeded(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, "[RateLimitExceeded]" + message)
            {
                rateLimitExceeded = true
            };
        }

        public Exception CreateExecutionNotAllowed(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, "[ExecutionNotAllowed]" + message)
            {
                executionNotAllowed = true
            };

        }

        public Exception CreateRequestSizeLimitExceeded(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, "[RequestSizeLimitExceeded]" + message)
            {
                requestSizeLimitExceeded = true
            };
        }

        public Exception CreateGeneral(string message)
        {
            return new ClusterScriptError(clusterScriptErrorConstructor, message);
        }

        public Exception CreateJsError(string message)
        {
            var instance = this.engine.Realm.Intrinsics.Error.Construct(message);
            return new Jint.Runtime.JavaScriptException(instance);
        }
    }
}
