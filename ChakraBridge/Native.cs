namespace ChakraBridge
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    ///     Native interfaces.
    /// </summary>
    public static class Native
    {
        [DllImport("Chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateRuntime(JavaScriptRuntimeAttributes attributes, 
            JavaScriptThreadServiceCallback threadService, out JavaScriptRuntime runtime);

        [DllImport("Chakra.dll")]
        public static extern JavaScriptErrorCode JsCollectGarbage(JavaScriptRuntime handle);

        [DllImport("Chakra.dll")]
        public static extern JavaScriptErrorCode JsDisposeRuntime(JavaScriptRuntime handle);

        [DllImport("Chakra.dll")]
        public static extern JavaScriptErrorCode JsGetRuntimeMemoryUsage(JavaScriptRuntime runtime, out UIntPtr memoryUsage);

        [DllImport("Chakra.dll")]
        public static extern JavaScriptErrorCode JsGetRuntimeMemoryLimit(JavaScriptRuntime runtime, out UIntPtr memoryLimit);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetRuntimeMemoryLimit(JavaScriptRuntime runtime, UIntPtr memoryLimit);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetRuntimeMemoryAllocationCallback(JavaScriptRuntime runtime, IntPtr callbackState, JavaScriptMemoryAllocationCallback allocationCallback);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetRuntimeBeforeCollectCallback(JavaScriptRuntime runtime, IntPtr callbackState, JavaScriptBeforeCollectCallback beforeCollectCallback);

        [DllImport("chakra.dll", EntryPoint = "JsAddRef")]
        public static extern JavaScriptErrorCode JsContextAddRef(JavaScriptContext reference, out uint count);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsAddRef(JavaScriptValue reference, out uint count);

        [DllImport("chakra.dll", EntryPoint = "JsRelease")]
        public static extern JavaScriptErrorCode JsContextRelease(JavaScriptContext reference, out uint count);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsRelease(JavaScriptValue reference, out uint count);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateContext(JavaScriptRuntime runtime, out JavaScriptContext newContext);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetCurrentContext(out JavaScriptContext currentContext);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetCurrentContext(JavaScriptContext context);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetRuntime(JavaScriptContext context, out JavaScriptRuntime runtime);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsStartDebugging();

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsIdle(out uint nextIdleTick);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsParseScript(string script, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsRunScript(string script, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsSerializeScript(string script, byte[] buffer, ref ulong bufferSize);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsParseSerializedScript(string script, byte[] buffer, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsRunSerializedScript(string script, byte[] buffer, JavaScriptSourceContext sourceContext, string sourceUrl, out JavaScriptValue result);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsGetPropertyIdFromName(string name, out JavaScriptPropertyId propertyId);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsGetPropertyNameFromId(JavaScriptPropertyId propertyId, out string name);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetUndefinedValue(out JavaScriptValue undefinedValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetNullValue(out JavaScriptValue nullValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetTrueValue(out JavaScriptValue trueValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetFalseValue(out JavaScriptValue falseValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsBoolToBoolean(bool value, out JavaScriptValue booleanValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsBooleanToBool(JavaScriptValue booleanValue, out bool boolValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsConvertValueToBoolean(JavaScriptValue value, out JavaScriptValue booleanValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetValueType(JavaScriptValue value, out JavaScriptValueType type);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsDoubleToNumber(double doubleValue, out JavaScriptValue value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsIntToNumber(int intValue, out JavaScriptValue value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsNumberToDouble(JavaScriptValue value, out double doubleValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsConvertValueToNumber(JavaScriptValue value, out JavaScriptValue numberValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetStringLength(JavaScriptValue sringValue, out int length);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsPointerToString(string value, UIntPtr stringLength, out JavaScriptValue stringValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsStringToPointer(JavaScriptValue value, out IntPtr stringValue, out UIntPtr stringLength);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsConvertValueToString(JavaScriptValue value, out JavaScriptValue stringValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsVariantToValue(ref object var, out JavaScriptValue value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsValueToVariant(JavaScriptValue obj, out object var);
        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsObjectToInspectable(JavaScriptValue obj, [MarshalAs(UnmanagedType.IInspectable)]out object inspectable);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetGlobalObject(out JavaScriptValue globalObject);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateObject(out JavaScriptValue obj);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateExternalObject(IntPtr data, JavaScriptObjectFinalizeCallback finalizeCallback, out JavaScriptValue obj);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsConvertValueToObject(JavaScriptValue value, out JavaScriptValue obj);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetPrototype(JavaScriptValue obj, out JavaScriptValue prototypeObject);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetPrototype(JavaScriptValue obj, JavaScriptValue prototypeObject);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetExtensionAllowed(JavaScriptValue obj, out bool value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsPreventExtension(JavaScriptValue obj);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, out JavaScriptValue value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetOwnPropertyDescriptor(JavaScriptValue obj, JavaScriptPropertyId propertyId, out JavaScriptValue propertyDescriptor);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetOwnPropertyNames(JavaScriptValue obj, out JavaScriptValue propertyNames);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, JavaScriptValue value, bool useStrictRules);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsHasProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, out bool hasProperty);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsDeleteProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, bool useStrictRules, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsDefineProperty(JavaScriptValue obj, JavaScriptPropertyId propertyId, JavaScriptValue propertyDescriptor, out bool result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsHasIndexedProperty(JavaScriptValue obj, JavaScriptValue index, out bool result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetIndexedProperty(JavaScriptValue obj, JavaScriptValue index, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetIndexedProperty(JavaScriptValue obj, JavaScriptValue index, JavaScriptValue value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsDeleteIndexedProperty(JavaScriptValue obj, JavaScriptValue index);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsEquals(JavaScriptValue obj1, JavaScriptValue obj2, out bool result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsStrictEquals(JavaScriptValue obj1, JavaScriptValue obj2, out bool result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsHasExternalData(JavaScriptValue obj, out bool value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetExternalData(JavaScriptValue obj, out IntPtr externalData);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetExternalData(JavaScriptValue obj, IntPtr externalData);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateArray(uint length, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCallFunction(JavaScriptValue function, JavaScriptValue[] arguments, ushort argumentCount, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsConstructObject(JavaScriptValue function, JavaScriptValue[] arguments, ushort argumentCount, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateFunction(JavaScriptNativeFunction nativeFunction, IntPtr externalData, out JavaScriptValue function);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateRangeError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateReferenceError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateSyntaxError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateTypeError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateURIError(JavaScriptValue message, out JavaScriptValue error);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsHasException(out bool hasException);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetAndClearException(out JavaScriptValue exception);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetException(JavaScriptValue exception);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsDisableRuntimeExecution(JavaScriptRuntime runtime);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsEnableRuntimeExecution(JavaScriptRuntime runtime);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsIsRuntimeExecutionDisabled(JavaScriptRuntime runtime, out bool isDisabled);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetObjectBeforeCollectCallback(JavaScriptValue reference, IntPtr callbackState, JavaScriptObjectBeforeCollectCallback beforeCollectCallback);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateNamedFunction(JavaScriptValue name, JavaScriptNativeFunction nativeFunction, IntPtr callbackState, out JavaScriptValue function);

        [DllImport("chakra.dll", CharSet = CharSet.Unicode)]
        public static extern JavaScriptErrorCode JsProjectWinRTNamespace(string namespaceName);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsInspectableToObject([MarshalAs(UnmanagedType.IInspectable)] System.Object inspectable, out JavaScriptValue value);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetPromiseContinuationCallback(JavaScriptPromiseContinuationCallback promiseContinuationCallback, IntPtr callbackState);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateArrayBuffer(uint byteLength, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateTypedArray(JavaScriptTypedArrayType arrayType, JavaScriptValue arrayBuffer, uint byteOffset,
            uint elementLength, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateDataView(JavaScriptValue arrayBuffer, uint byteOffset, uint byteOffsetLength, out JavaScriptValue result);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetArrayBufferStorage(JavaScriptValue arrayBuffer, out byte[] buffer, out uint bufferLength);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetTypedArrayStorage(JavaScriptValue typedArray, out byte[] buffer, out uint bufferLength, out JavaScriptTypedArrayType arrayType, out int elementSize);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetDataViewStorage(JavaScriptValue dataView, out byte[] buffer, out uint bufferLength);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetPropertyIdType(JavaScriptPropertyId propertyId, out JavaSciptPropertyIdType propertyIdType);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsCreateSymbol(JavaScriptValue description, out JavaScriptValue symbol);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetSymbolFromPropertyId(JavaScriptPropertyId propertyId, out JavaScriptValue symbol);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetPropertyIdFromSymbol(JavaScriptValue symbol, out JavaScriptPropertyId propertyId);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetOwnPropertySymbols(JavaScriptValue obj, out JavaScriptValue propertySymbols);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsNumberToInt(JavaScriptValue value, out int intValue);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsSetIndexedPropertiesToExternalData(JavaScriptValue obj, IntPtr data, JavaScriptTypedArrayType arrayType, uint elementLength);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsGetIndexedPropertiesExternalData(JavaScriptValue obj, IntPtr data, out JavaScriptTypedArrayType arrayType, out uint elementLength);

        [DllImport("chakra.dll")]
        public static extern JavaScriptErrorCode JsHasIndexedPropertiesExternalData(JavaScriptValue obj, out bool value);


        /// <summary>
        /// Throws if a native method returns an error code.
        /// </summary>
        /// <param name="error">The error.</param>
        public static void ThrowIfError(JavaScriptErrorCode error)
        {
            if (error != JavaScriptErrorCode.NoError)
            {
                switch (error)
                {
                    case JavaScriptErrorCode.InvalidArgument:
                        throw new JavaScriptUsageException(error, "Invalid argument.");

                    case JavaScriptErrorCode.NullArgument:
                        throw new JavaScriptUsageException(error, "Null argument.");

                    case JavaScriptErrorCode.NoCurrentContext:
                        throw new JavaScriptUsageException(error, "No current context.");

                    case JavaScriptErrorCode.InExceptionState:
                        throw new JavaScriptUsageException(error, "Runtime is in exception state.");

                    case JavaScriptErrorCode.NotImplemented:
                        throw new JavaScriptUsageException(error, "Method is not implemented.");

                    case JavaScriptErrorCode.WrongThread:
                        throw new JavaScriptUsageException(error, "Runtime is active on another thread.");

                    case JavaScriptErrorCode.RuntimeInUse:
                        throw new JavaScriptUsageException(error, "Runtime is in use.");

                    case JavaScriptErrorCode.BadSerializedScript:
                        throw new JavaScriptUsageException(error, "Bad serialized script.");

                    case JavaScriptErrorCode.InDisabledState:
                        throw new JavaScriptUsageException(error, "Runtime is disabled.");

                    case JavaScriptErrorCode.CannotDisableExecution:
                        throw new JavaScriptUsageException(error, "Cannot disable execution.");

                    case JavaScriptErrorCode.AlreadyDebuggingContext:
                        throw new JavaScriptUsageException(error, "Context is already in debug mode.");

                    case JavaScriptErrorCode.HeapEnumInProgress:
                        throw new JavaScriptUsageException(error, "Heap enumeration is in progress.");

                    case JavaScriptErrorCode.ArgumentNotObject:
                        throw new JavaScriptUsageException(error, "Argument is not an object.");

                    case JavaScriptErrorCode.InProfileCallback:
                        throw new JavaScriptUsageException(error, "In a profile callback.");

                    case JavaScriptErrorCode.InThreadServiceCallback:
                        throw new JavaScriptUsageException(error, "In a thread service callback.");

                    case JavaScriptErrorCode.CannotSerializeDebugScript:
                        throw new JavaScriptUsageException(error, "Cannot serialize a debug script.");

                    case JavaScriptErrorCode.AlreadyProfilingContext:
                        throw new JavaScriptUsageException(error, "Already profiling this context.");

                    case JavaScriptErrorCode.IdleNotEnabled:
                        throw new JavaScriptUsageException(error, "Idle is not enabled.");

                    case JavaScriptErrorCode.OutOfMemory:
                        throw new JavaScriptEngineException(error, "Out of memory.");

                    case JavaScriptErrorCode.ScriptException:
                        {
                            JavaScriptValue errorObject;
                            JavaScriptErrorCode innerError = JsGetAndClearException(out errorObject);

                            if (innerError != JavaScriptErrorCode.NoError)
                            {
                                throw new JavaScriptFatalException(innerError);
                            }

                            throw new JavaScriptScriptException(error, errorObject, "Script threw an exception.");
                        }

                    case JavaScriptErrorCode.ScriptCompile:
                        {
                            JavaScriptValue errorObject;
                            JavaScriptErrorCode innerError = JsGetAndClearException(out errorObject);

                            if (innerError != JavaScriptErrorCode.NoError)
                            {
                                throw new JavaScriptFatalException(innerError);
                            }

                            throw new JavaScriptScriptException(error, errorObject, "Compile error.");
                        }

                    case JavaScriptErrorCode.ScriptTerminated:
                        throw new JavaScriptScriptException(error, JavaScriptValue.Invalid, "Script was terminated.");

                    case JavaScriptErrorCode.ScriptEvalDisabled:
                        throw new JavaScriptScriptException(error, JavaScriptValue.Invalid, "Eval of strings is disabled in this runtime.");

                    case JavaScriptErrorCode.Fatal:
                        throw new JavaScriptFatalException(error);

                    default:
                        throw new JavaScriptFatalException(error);
                }
            }
        }
    }
}
