namespace Hospital.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgreSqlHospitalApiFactory>
{
    public const string Name = "Integration API tests";
}
