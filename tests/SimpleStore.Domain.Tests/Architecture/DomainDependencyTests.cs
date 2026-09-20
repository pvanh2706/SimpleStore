using SimpleStore.Domain;
using Xunit;

namespace SimpleStore.Domain.Tests.Architecture;

public sealed class DomainDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterLayersOrFrameworks()
    {
        var forbiddenReferences = new[]
        {
            "SimpleStore.Api",
            "SimpleStore.Application",
            "SimpleStore.Infrastructure",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore"
        };

        var referencedAssemblies = typeof(DomainAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .ToArray();

        foreach (var forbiddenReference in forbiddenReferences)
        {
            Assert.DoesNotContain(
                referencedAssemblies,
                name => name!.StartsWith(forbiddenReference, StringComparison.Ordinal));
        }
    }
}
