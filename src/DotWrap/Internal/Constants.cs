namespace DotWrap.Internal;

internal static class Constants
{
    public const string InternalPrefix = "__dotwrap";
    public const string SelfPtrType = "IntPtr";
    public const string SelfPointerName = InternalPrefix + "SelfPtr";
    public const string Create = InternalPrefix + "Create";
    public const string Get = InternalPrefix + "Get";
    public const string Destroy = InternalPrefix + "Destroy";
    public const string Metadata = InternalPrefix + "Metadata";
    public const string Obj = InternalPrefix + "Obj";
    public const string Typed = InternalPrefix + "Typed";
    public const string InternalResult = InternalPrefix + "Result";
    public const string ExportedResult = InternalPrefix + "ExResult";
    public const string GetCount = InternalPrefix + "GetCount";
    public const string FillArr = InternalPrefix + "FillArr";
    public const string OutParam = InternalPrefix + "Out";
}
