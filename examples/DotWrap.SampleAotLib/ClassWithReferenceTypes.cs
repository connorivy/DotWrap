using System;

namespace DotWrap.SampleAotLib;

[DotWrapExpose]
public class ClassWithReferenceTypes
{
    public SampleClass CreateSampleClass()
    {
        return new SampleClass();
    }

    public bool AcceptSampleClass(SampleClass sample)
    {
        return sample != null;
    }

    public object CreateObject()
    {
        return new object();
    }

    public bool AcceptObject(object obj)
    {
        return obj != null;
    }
}
