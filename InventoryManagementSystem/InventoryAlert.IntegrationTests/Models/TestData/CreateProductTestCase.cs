using InventoryAlert.IntegrationTests.Models.Request;

namespace InventoryAlert.IntegrationTests.Models.TestData;

public class CreateProductTestCase
{
    public string TestCaseName { get; set; } = string.Empty;
    public CreateUpdateProductRequest Request { get; set; } = new CreateUpdateProductRequest();
    public int ExpectedStatusCode { get; set; }

    public override string ToString()
    {
        return TestCaseName;
    }
}
