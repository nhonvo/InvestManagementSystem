using InventoryAlert.IntegrationTests.Config;
using InventoryAlert.IntegrationTests.Models.Request;
using InventoryAlert.IntegrationTests.Models.Response;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class ProductClient(RestClient client) : BaseClient(client)
{
    public async Task<RestResponse<ListResponse<ProductResponse>>> GetProductsAsync(GetProductsRequest parameters)
    {
        var request = new RestRequest("/api/v1/Products");
        request.AddQueryParameter("PageNumber", parameters.PageNumber.ToString());
        request.AddQueryParameter("PageSize", parameters.PageSize.ToString());

        if (!string.IsNullOrEmpty(parameters.Name))
            request.AddQueryParameter("Name", parameters.Name);

        if (!string.IsNullOrEmpty(parameters.MinStock))
            request.AddQueryParameter("MinStock", parameters.MinStock);

        if (!string.IsNullOrEmpty(parameters.MaxStock))
            request.AddQueryParameter("MaxStock", parameters.MaxStock);

        if (!string.IsNullOrEmpty(parameters.SortBy))
            request.AddQueryParameter("SortBy", parameters.SortBy);

        return await _client.ExecuteGetAsync<ListResponse<ProductResponse>>(request);
    }

    public async Task<RestResponse<ProductResponse>> GetProductByIdAsync(int productId)
    {
        var request = new RestRequest("/api/v1/Products/{productId}");
        request.AddUrlSegment("productId", productId.ToString());
        return await _client.ExecuteGetAsync<ProductResponse>(request);
    }

    public async Task<RestResponse<ProductResponse>> CreateProductAsync(CreateUpdateProductRequest createRequest)
    {
        var request = new RestRequest("/api/v1/Products");
        request.AddJsonBody(createRequest);
        return await _client.ExecutePostAsync<ProductResponse>(request);
    }

    public async Task<RestResponse<ProductResponse>> UpdateProductAsync(int productId, CreateUpdateProductRequest updateRequest)
    {
        var request = new RestRequest("/api/v1/Products/{productId}");
        request.AddUrlSegment("productId", productId.ToString());
        request.AddJsonBody(updateRequest);
        return await _client.ExecutePutAsync<ProductResponse>(request);
    }

    public async Task<RestResponse<ProductResponse>> DeleteProductAsync(int productId)
    {
        var request = new RestRequest("/api/v1/Products/{productId}");
        request.AddUrlSegment("productId", productId.ToString());
        return await _client.ExecuteDeleteAsync<ProductResponse>(request);
    }

    public async Task<RestResponse> TriggerPriceAlertAsync()
    {
        var request = new RestRequest("/api/v1/Products/sync-price");
        return await _client.ExecutePostAsync(request);
    }
}
