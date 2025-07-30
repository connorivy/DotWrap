# How It Works

The interop layer that is created in 4 different phases

## 1. Compile

This phase happens as you write you C# code in a modern IDE such as VSCode, VS, Neovim, Rider, etc.

DotWrap includes a source generator that is notified when you mark a class with the `DotWrapExpose` attribute. It then generates something like the following

```csharp
namespace CoolCalc
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [global::System.CodeDom.Compiler.GeneratedCode("DotWrap", "1.0.0")]
    [global::DotWrap.DotWrapGenerated]
    internal static class CalculatorDotWrapWrapper
    {

        internal static IntPtr __dotwrapCreate(Calculator __dotwrapObj)
        {
            var handle = GCHandle.Alloc(__dotwrapObj, GCHandleType.Normal);
            return GCHandle.ToIntPtr(handle);
        }

        internal static Calculator __dotwrapGet(IntPtr __dotwrapSelfPtr)
        {
            var handle = GCHandle.FromIntPtr(__dotwrapSelfPtr);
            if (!handle.IsAllocated) throw new System.ArgumentException($"Invalid handle: {__dotwrapSelfPtr}");
            var __dotwrapObj = (Calculator)handle.Target;
            return __dotwrapObj;
        }


        [UnmanagedCallersOnly(EntryPoint = "CoolCalc_Calculator___dotwrapDestroy")]
        public static void __dotwrapDestroy(IntPtr __dotwrapSelfPtr)
        {
            var handle = GCHandle.FromIntPtr(__dotwrapSelfPtr);
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "CoolCalc_Calculator_Add_1FD58AC2")]
        public static int Add_1FD58AC2(IntPtr __dotwrapSelfPtr, int a, int b)
        {
            var __dotwrapObj = __dotwrapGet(__dotwrapSelfPtr);
            var __dotwrapResult = __dotwrapObj.Add(a, b);
            return __dotwrapResult;
        }

        [UnmanagedCallersOnly(EntryPoint = "CoolCalc_Calculator_Constructor")]
        public static IntPtr Constructor()
        {
            var __dotwrapResult = new Calculator();

            var __dotwrapExResult = CoolCalc.CalculatorDotWrapWrapper.__dotwrapCreate(__dotwrapResult);
            return __dotwrapExResult;
        }


#pragma warning disable CS0414 // Field is assigned to but its value is never used
        private static readonly string __dotwrapMetadata =
        """
        {"Namespace":"CoolCalc","ClassName":"Calculator","EntryPrefix":"CoolCalc_Calculator_","IsStatic":false,"GenericTypeParametersToArguments":{},"Interfaces":[],"SpecialCaseFlags":0,"SummaryComment":null,"Methods":[{"OriginalName":"Add","StampedName":"Add_1FD58AC2","OriginalType":"int","ExposedTypeIfDifferent":null,"GenericTypeName":null,"IsStatic":false,"SummaryComment":null,"ReturnsComment":null,"SpecialCaseFlags":0,"Parameters":[{"Name":"a","OriginalType":"int","ExposedTypeIfDifferent":null,"GenericTypeName":null,"Comment":null},{"Name":"b","OriginalType":"int","ExposedTypeIfDifferent":null,"GenericTypeName":null,"Comment":null}]},{"OriginalName":"Constructor","StampedName":"Constructor","OriginalType":"CoolCalc.Calculator","ExposedTypeIfDifferent":"IntPtr","GenericTypeName":null,"IsStatic":true,"SummaryComment":null,"ReturnsComment":null,"SpecialCaseFlags":0,"Parameters":[]}],"Properties":[]}
        """;
#pragma warning restore CS0414 // Field is assigned to but its value is never used
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
```

There are a few parts to the generated class. - Memory management methods (**dotwrapCreate, **dotwrapDestroy) - These methods handle exposing managed c# objects as pointers to python and retreiving original objects from pointers - Native export methods (Add_1FD58AC2) - These methods are the exposed methods that will show up you python classes. - They are fingerprinted in order to avoid overload collisions in c#. The fingerprint will be removed in the python code - Metadata field (\_\_dotwrapMetadata) - The field exposes important information about the original type to the python code generator - This field is private and unread which means it will definitely be trimmed out of your published library

## 1. AfterBuild

After you build you project, several things will be generated in a `python_project_root` dir.

    - Main.Py

```python
class Calculator:
    def add(self, a: int, b: int) -> int:
        return _dotwrap_lib.CoolCalc_Calculator_Add_1FD58AC2(self._dotwrap_ptr, a, b)

    def __init__(self) -> None:
        self._dotwrap_ptr = _dotwrap_lib.CoolCalc_Calculator_Constructor()

    @classmethod
    def _dotwrap_from_ptr(cls, ptr: int):
        instance = object.__new__(cls)
        instance._dotwrap_ptr = ptr
        return instance

    def __del__(self):
        _dotwrap_lib.CoolCalc_Calculator___dotwrapDestroy(self._dotwrap_ptr)

```

Python file with the classes that wrap you c# exposed classes

    - MyProject.h

```c
#ifndef DOTWRAP_MyProject_H
#define DOTWRAP_MyProject_H
void DotWrap_BuiltIn_CString_Free(void* ptr);
void CoolCalc_Calculator___dotwrapDestroy(void* ptr);
int32_t CoolCalc_Calculator_Add_1FD58AC2(void* ptr, int32_t a, int32_t b);
void* CoolCalc_Calculator_Constructor();
#endif // DOTWRAP_MyProject_H
```

A header file that defines the native exports in you lib

    - lib_build.py

```python
from cffi import FFI
from typing import Any
import os

ffibuilder = FFI()
ffibuilder.cdef("""
void DotWrap_BuiltIn_CString_Free(void* ptr);

void CoolCalc_Calculator___dotwrapDestroy(void* ptr);
int32_t CoolCalc_Calculator_Add_1FD58AC2(void* ptr, int32_t a, int32_t b);
void* CoolCalc_Calculator_Constructor();
""")

current_dir = os.path.dirname(os.path.abspath(__file__))
ffibuilder.set_source(
    "_MyProject",
    """
    #include "MyProject.h"
    """,
    libraries=["MyProject"],
    library_dirs=[current_dir],
    include_dirs=[current_dir],
)

if __name__ == '__main__':
    ffibuilder.compile(verbose=True, tmpdir=current_dir)

```

this lib_build.py uses a python library called cffi to create CPython bindings to the methods specified in the `cdef` method.

## 1. AfterPublish

    - Copies relevant publish artifacts into the python package

## 1. BeforePipInstall

Before pip install runs, it automatically runs the generated lib_build.py file. The result is a .c file that will look something like this

```c
static int32_t _cffi_d_CoolCalc_Calculator_Add_1FD58AC2(void * x0, int32_t x1, int32_t x2)
{
  return CoolCalc_Calculator_Add_1FD58AC2(x0, x1, x2);
}
#ifndef PYPY_VERSION
static PyObject *
_cffi_f_CoolCalc_Calculator_Add_1FD58AC2(PyObject *self, PyObject *args)
{
  void * x0;
  int32_t x1;
  int32_t x2;
  Py_ssize_t datasize;
  struct _cffi_freeme_s *large_args_free = NULL;
  int32_t result;
  PyObject *pyresult;
  PyObject *arg0;
  PyObject *arg1;
  PyObject *arg2;

  if (!PyArg_UnpackTuple(args, "CoolCalc_Calculator_Add_1FD58AC2", 3, 3, &arg0, &arg1, &arg2))
    return NULL;

  datasize = _cffi_prepare_pointer_call_argument(
      _cffi_type(9), arg0, (char **)&x0);
  if (datasize != 0) {
    x0 = ((size_t)datasize) <= 640 ? (void *)alloca((size_t)datasize) : NULL;
    if (_cffi_convert_array_argument(_cffi_type(9), arg0, (char **)&x0,
            datasize, &large_args_free) < 0)
      return NULL;
  }

  x1 = _cffi_to_c_int(arg1, int32_t);
  if (x1 == (int32_t)-1 && PyErr_Occurred())
    return NULL;

  x2 = _cffi_to_c_int(arg2, int32_t);
  if (x2 == (int32_t)-1 && PyErr_Occurred())
    return NULL;

  Py_BEGIN_ALLOW_THREADS
  _cffi_restore_errno();
  { result = CoolCalc_Calculator_Add_1FD58AC2(x0, x1, x2); }
  _cffi_save_errno();
  Py_END_ALLOW_THREADS

  (void)self; /* unused */
  pyresult = _cffi_from_c_int(result, int32_t);
  if (large_args_free != NULL) _cffi_free_array_arguments(large_args_free);
  return pyresult;
}
#else
#  define _cffi_f_CoolCalc_Calculator_Add_1FD58AC2 _cffi_d_CoolCalc_Calculator_Add_1FD58AC2
#endif
```
