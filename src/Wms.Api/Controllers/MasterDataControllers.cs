using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Services;

namespace Wms.Api.Controllers;

/// <summary>物料主檔。</summary>
public class MaterialsController(IMaterialService service) : ApiControllerBase
{
    /// <summary>分頁查詢物料。</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.MaterialView)]
    public async Task<ApiResponse<PagedResult<MaterialDto>>> Query([FromQuery] MaterialQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    /// <summary>以掃碼結果（條碼或物料編號）查詢物料。</summary>
    [HttpGet("by-barcode/{barcode}")]
    [Authorize(Policy = Permissions.MaterialView)]
    public async Task<ApiResponse<MaterialDto?>> GetByBarcode(string barcode, CancellationToken ct)
    {
        var material = await service.GetByBarcodeAsync(barcode, ct);
        return material is null
            ? ApiResponse<MaterialDto?>.Fail("查無此條碼對應的物料。", new ApiError("NOT_FOUND", barcode))
            : Success<MaterialDto?>(material);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.MaterialView)]
    public async Task<ApiResponse<MaterialDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.MaterialCreate)]
    public async Task<ApiResponse<MaterialDto>> Create(MaterialSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "物料已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.MaterialUpdate)]
    public async Task<ApiResponse<MaterialDto>> Update(Guid id, MaterialSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "物料已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.MaterialDelete)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("物料已刪除。");
    }
}

/// <summary>物料分類。</summary>
[Route("api/v1/material-categories")]
public class MaterialCategoriesController(IMaterialCategoryService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CategoryView)]
    public async Task<ApiResponse<PagedResult<MaterialCategoryDto>>> Query([FromQuery] PagedQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CategoryView)]
    public async Task<ApiResponse<MaterialCategoryDto>> Get(Guid id, CancellationToken ct)
        => Success(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.CategoryManage)]
    public async Task<ApiResponse<MaterialCategoryDto>> Create(MaterialCategorySaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "物料分類已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CategoryManage)]
    public async Task<ApiResponse<MaterialCategoryDto>> Update(Guid id, MaterialCategorySaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "物料分類已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.CategoryManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("物料分類已刪除。");
    }
}

/// <summary>計量單位。</summary>
[Route("api/v1/uoms")]
public class UomsController(IUomService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.UomView)]
    public async Task<ApiResponse<PagedResult<UomDto>>> Query([FromQuery] PagedQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.UomManage)]
    public async Task<ApiResponse<UomDto>> Create(UomSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "單位已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UomManage)]
    public async Task<ApiResponse<UomDto>> Update(Guid id, UomSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "單位已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.UomManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("單位已刪除。");
    }
}

/// <summary>物料條碼。</summary>
[Route("api/v1/barcodes")]
public class BarcodesController(IBarcodeService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.BarcodeView)]
    public async Task<ApiResponse<PagedResult<MaterialBarcodeDto>>> Query([FromQuery] BarcodeQuery query, CancellationToken ct)
        => Success(await service.QueryAsync(query, ct));

    [HttpPost]
    [Authorize(Policy = Permissions.BarcodeManage)]
    public async Task<ApiResponse<MaterialBarcodeDto>> Create(MaterialBarcodeSaveDto dto, CancellationToken ct)
        => Success(await service.CreateAsync(dto, ct), "條碼已建立。");

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.BarcodeManage)]
    public async Task<ApiResponse<MaterialBarcodeDto>> Update(Guid id, MaterialBarcodeSaveDto dto, CancellationToken ct)
        => Success(await service.UpdateAsync(id, dto, ct), "條碼已更新。");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.BarcodeManage)]
    public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Success("條碼已刪除。");
    }
}
