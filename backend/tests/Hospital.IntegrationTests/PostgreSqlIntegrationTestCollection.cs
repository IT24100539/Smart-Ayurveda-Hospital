using Xunit;

namespace Hospital.IntegrationTests;

[CollectionDefinition(
    "PostgreSQL integration tests",
    DisableParallelization = true)]
public sealed class PostgreSqlIntegrationTestCollection
    : ICollectionFixture<PostgreSqlHospitalApiFactory>
{
    public const string Name = "PostgreSQL integration tests";
}