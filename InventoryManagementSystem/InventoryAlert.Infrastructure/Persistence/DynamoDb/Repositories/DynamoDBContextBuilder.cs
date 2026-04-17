using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Infrastructure.Persistence.DynamoDb.Repositories;

public class DynamoDBContextBuilder
{
    private Func<IAmazonDynamoDB> _clientFactory = null!;

    public DynamoDBContextBuilder WithDynamoDBClient(Func<IAmazonDynamoDB> clientFactory)
    {
        _clientFactory = clientFactory;
        return this;
    }

    public IDynamoDBContext Build()
    {
#pragma warning disable CS0618
        return new DynamoDBContext(_clientFactory());
#pragma warning restore CS0618
    }
}
