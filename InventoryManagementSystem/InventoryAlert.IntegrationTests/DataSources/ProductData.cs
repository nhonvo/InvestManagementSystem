using InventoryAlert.IntegrationTests.Models.TestData;
using Newtonsoft.Json;

namespace InventoryAlert.IntegrationTests.DataSources;

public static class ProductData
{
    private static string CreateProductDataFilePath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, 
        "TestData", 
        "Product", 
        "create-product-cases.json"
    );

    public static IEnumerable<object[]> GetCreateProductTestCases()
    {
        var jsonData = File.ReadAllText(CreateProductDataFilePath);
        List<CreateProductTestCase> testCases = JsonConvert.DeserializeObject<List<CreateProductTestCase>>(jsonData) ?? new List<CreateProductTestCase>();
        foreach (CreateProductTestCase testCase in testCases)
        {
            yield return new object[] { testCase };
        }
    }
}
