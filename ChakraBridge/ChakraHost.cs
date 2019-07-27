using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace ChakraBridge
{
    public sealed class ChakraHost : IDisposable
    {
        private JavaScriptSourceContext currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);
        private readonly JavaScriptRuntime runtime;
        private readonly JavaScriptContext context;
        private JavaScriptValue promiseCallback;
        private bool _isContextSet, _noRemove;
        public ChakraHost(bool debug = false, bool removeContext = true)
        {
            if (Native.JsCreateRuntime(JavaScriptRuntimeAttributes.None, null, out runtime) !=
                JavaScriptErrorCode.NoError)
            {
                throw new Exception("failed to create runtime.");
            }

            if (Native.JsCreateContext(runtime, out context) != JavaScriptErrorCode.NoError)
                throw new Exception("failed to create execution context.");
            if (debug)
            {
                try
                {
                    SetContextOnCurrentThread();
                    Native.ThrowIfError(Native.JsStartDebugging());
                }
                finally
                {
                    if (removeContext)
                        RemoveContextOnCurrentThread();
                }
            }

        }
        public void SetContextOnCurrentThread()
        {
            if (_isContextSet)
            {
                _noRemove = true;
                return;
            }
            if (Native.JsSetCurrentContext(context) != JavaScriptErrorCode.NoError)
                throw new Exception("failed to set current context.");
            _isContextSet = true;
        }
        public void RemoveContextOnCurrentThread()
        {
            if (!_isContextSet)
                return;
            if (_noRemove)
            {
                _noRemove = false;
                return;
            }
            if (Native.JsSetCurrentContext(new JavaScriptContext()) != JavaScriptErrorCode.NoError)
                throw new Exception("failed to remove current context");
            _isContextSet = false;
        }
        public void ProjectNamespace(string namespaceName)
        {
            try
            {
                SetContextOnCurrentThread();
                if (Native.JsProjectWinRTNamespace(namespaceName) != JavaScriptErrorCode.NoError)
                    throw new Exception($"failed to project {namespaceName} namespace.");
            }
            finally
            {
                RemoveContextOnCurrentThread();
            }
        }

        public string RunScript(string script, string sourcefilePath)
        {
            try
            {
                SetContextOnCurrentThread();

                if (Native.JsRunScript(
                    script,
                    currentSourceContext++,
                    sourcefilePath,
                    out JavaScriptValue result) != JavaScriptErrorCode.NoError)
                {
                    // Get error message and clear exception
                    if (Native.JsGetAndClearException(out JavaScriptValue exception) != JavaScriptErrorCode.NoError)
                        throw new Exception("failed to get and clear exception");

                    if (Native.JsGetPropertyIdFromName(
                        "message",
                        out JavaScriptPropertyId messageName) != JavaScriptErrorCode.NoError)
                        throw new Exception("failed to get error message id");

                    if (Native.JsGetProperty(
                        exception,
                        messageName,
                        out JavaScriptValue messageValue) != JavaScriptErrorCode.NoError)
                        throw new Exception("failed to get error message");

                    if (Native.JsStringToPointer(
                        messageValue,
                        out IntPtr message,
                        out UIntPtr length) != JavaScriptErrorCode.NoError)
                        throw new Exception("failed to convert error message");

                    return Marshal.PtrToStringUni(message);
                }

                // Execute promise tasks stored in promiseCallback 
                while (promiseCallback.IsValid)
                {
                    JavaScriptValue task = promiseCallback;
                    promiseCallback = JavaScriptValue.Invalid;
                    Native.JsCallFunction(task, null, 0, out JavaScriptValue promiseResult);
                }

                // Convert the return value.
                if (Native.JsConvertValueToString(
                    result,
                    out JavaScriptValue stringResult) != JavaScriptErrorCode.NoError)
                {
                    throw new Exception("failed to convert value to string.");
                }
                if (Native.JsStringToPointer(
                    stringResult,
                    out IntPtr returnValue,
                    out UIntPtr stringLength) !=
                    JavaScriptErrorCode.NoError)
                {
                    throw new Exception("failed to convert return value.");
                }

                return Marshal.PtrToStringUni(returnValue);
            }
            finally
            {
                RemoveContextOnCurrentThread();
            }
        }

        public string ProjectObjectToGlobal(object objectToProject, string name)
        {
            try
            {
                SetContextOnCurrentThread();
                if (Native.JsInspectableToObject(
                    objectToProject,
                    out JavaScriptValue value) != JavaScriptErrorCode.NoError)
                {
                    return $"failed to project {name} object";
                }

                DefineHostProperty(name, value);

                return "NoError";
            }
            finally
            {
                RemoveContextOnCurrentThread();
            }
        }

        public object CallFunction(string name, params object[] parameters)
        {
            try
            {
                SetContextOnCurrentThread();
                Native.JsGetGlobalObject(out JavaScriptValue globalObject);

                var functionId = JavaScriptPropertyId.FromString(name);

                var function = globalObject.GetProperty(functionId);

                // Parameters
                var javascriptParameters = new List<JavaScriptValue>
                {
                    globalObject // this value
                };
                foreach (var parameter in parameters)
                {
                    var parameterType = parameter.GetType().Name;
                    switch (parameterType)
                    {
                        case "Int32":
                            javascriptParameters.Add(JavaScriptValue.FromInt32((int)parameter));
                            break;
                        case "Double":
                            javascriptParameters.Add(JavaScriptValue.FromDouble((double)parameter));
                            break;
                        case "Boolean":
                            javascriptParameters.Add(JavaScriptValue.FromBoolean((bool)parameter));
                            break;
                        case "String":
                            javascriptParameters.Add(JavaScriptValue.FromString((string)parameter));
                            break;
                        default:
                            //throw new Exception("Not supported type: " + parameterType);
                            javascriptParameters.Add(JavaScriptValue.FromInspectable(parameter));
                            break;
                    }
                }
                var retval = function.CallFunction(javascriptParameters.ToArray());
                Native.ThrowIfError(
                    Native.JsGetValueType(
                        retval,
                        out JavaScriptValueType type));
                switch (type)
                {
                    case JavaScriptValueType.Array:
                        return retval;
                    case JavaScriptValueType.String:
                        return retval.ToString();
                    case JavaScriptValueType.Null:
                    case JavaScriptValueType.Undefined:
                    default:
                        return null;
                }
            }
            finally
            {
                RemoveContextOnCurrentThread();
            }
        }
        public T[] JsArrayToArray<T>(object jsvalue)
        {
            try
            {
                SetContextOnCurrentThread();
                if (!(jsvalue is JavaScriptValue))
                    return null;
                var arrayval = (JavaScriptValue)jsvalue;

                Native.ThrowIfError(
                    Native.JsGetValueType(
                        arrayval,
                        out JavaScriptValueType type));

                if (type != JavaScriptValueType.Array)
                    return null;

                Native.ThrowIfError(
                    Native.JsGetProperty(
                        arrayval,
                        JavaScriptPropertyId.FromString("length"),
                        out JavaScriptValue lengthvalue));

                Native.ThrowIfError(
                    Native.JsNumberToInt(
                        lengthvalue,
                        out int length));

                var _retArray = new T[length];

                for (int i = 0; i < length; i++)
                {
                    Native.ThrowIfError(
                        Native.JsGetIndexedProperty(
                            arrayval,
                            JavaScriptValue.FromInt32(i),
                            out JavaScriptValue elem));

                    Native.ThrowIfError(
                        Native.JsGetValueType(
                            elem,
                            out JavaScriptValueType elemtype));

                    if (elemtype == JavaScriptValueType.Object)
                    {
                        Native.ThrowIfError(
                            Native.JsObjectToInspectable(
                                elem, 
                                out object insp));
                        if (insp.GetType() == typeof(T))
                            _retArray[i] = (T)insp;
                    }
                }
                return _retArray;
            }
            finally
            {
                RemoveContextOnCurrentThread();
            }
        }

        public List<T> JsArrayToList<T>(object jsvalue)
        {
            try
            {
                SetContextOnCurrentThread();
                if (!(jsvalue is JavaScriptValue))
                    return null;
                var arrayval = (JavaScriptValue)jsvalue;
                var _retList = new List<T>();

                Native.ThrowIfError(Native.JsGetValueType(arrayval, out JavaScriptValueType type));
                if (type != JavaScriptValueType.Array)
                    return null;

                Native.ThrowIfError(
                    Native.JsGetProperty(
                        arrayval,
                        JavaScriptPropertyId.FromString("length"),
                        out JavaScriptValue lengthvalue));

                Native.ThrowIfError(Native.JsNumberToInt(lengthvalue, out int length));

                for (int i = 0; i < length; i++)
                {
                    Native.ThrowIfError(
                        Native.JsGetIndexedProperty(
                            arrayval,
                            JavaScriptValue.FromInt32(i),
                            out JavaScriptValue elem));

                    Native.ThrowIfError(
                        Native.JsGetValueType(
                            elem, 
                            out JavaScriptValueType elemtype));

                    if (elemtype == JavaScriptValueType.Object)
                    {
                        var err = Native.JsObjectToInspectable(elem, out object insp);
                        if (err == JavaScriptErrorCode.NoError &&
                                insp.GetType() == typeof(T))
                        {
                            _retList.Add((T)insp);
                        }
                    }

                }
                return _retList;
            }
            finally
            {
                RemoveContextOnCurrentThread();
            }
        }
        // Private tools
        private static void DefineHostCallback(string callbackName, JavaScriptNativeFunction callback)
        {
            Native.JsGetGlobalObject(out JavaScriptValue globalObject);

            var propertyId = JavaScriptPropertyId.FromString(callbackName);
            var function = JavaScriptValue.CreateFunction(callback, IntPtr.Zero);

            globalObject.SetProperty(propertyId, function, true);

            Native.JsAddRef(function, out uint refCount);
        }

        private static void DefineHostProperty(string callbackName, JavaScriptValue value)
        {
            Native.JsGetGlobalObject(out JavaScriptValue globalObject);

            var propertyId = JavaScriptPropertyId.FromString(callbackName);
            globalObject.SetProperty(propertyId, value, true);

            Native.JsAddRef(value, out uint refCount);
        }

        public void Dispose()
        {
            if (_isContextSet)
            {
                RemoveContextOnCurrentThread();
            }
            Native.ThrowIfError(Native.JsDisposeRuntime(runtime));
        }
    }
}