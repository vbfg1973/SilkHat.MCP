namespace SilkHat.Code.Analysis.Graph
{
    public enum GraphNodeKind
    {
        Solution,
        Project,
        Package,
        PackageVersion,
        GitAuthor,
        GitCommit,
        Folder,
        File,
        NamedType,
        Field,
        Property,
        Method,
        ComplexityMetric,
        Parameter
    }
}