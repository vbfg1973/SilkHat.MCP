namespace SilkHat.Code.Analysis.Graph
{
    public enum EdgeType
    {
        Contains,
        DeclaresType,
        DeclaresMember,
        DeclaresParameter,
        Inherits,
        Implements,
        ImplementsMember,
        Calls,
        Changes,
        AuthoredBy,
        DependsOnPackage,
        PackageVersion,
        ExternalReference,
        HasMetric,
        PropertyType,
        FieldType,
        ReturnType,
        Parameter,
        ParameterType,
        MethodCall
    }
}