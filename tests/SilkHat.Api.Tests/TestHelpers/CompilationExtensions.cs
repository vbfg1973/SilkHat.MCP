using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis
{
    public static class CompilationExtensions
    {
        public static Task<EmitResult> EmitAsync(
            this Compilation compilation,
            Stream peStream,
            CancellationToken cancellationToken = default)
        {
            if (compilation is null) throw new ArgumentNullException(nameof(compilation));

            if (peStream is null) throw new ArgumentNullException(nameof(peStream));

            return Task.FromResult(compilation.Emit(peStream, cancellationToken: cancellationToken));
        }
    }
}