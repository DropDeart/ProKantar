using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.WeighingPhotos;

/// <summary>Kimliğe göre tek bir WeighingPhoto getirir.</summary>
public sealed class GetWeighingPhotoByIdQuery : IQuery<WeighingPhotoDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetWeighingPhotoByIdHandler : IQueryHandler<GetWeighingPhotoByIdQuery, WeighingPhotoDto?>
{
    private readonly IRepository<WeighingPhoto> _repository;

    public GetWeighingPhotoByIdHandler(IRepository<WeighingPhoto> repository) => _repository = repository;

    public async Task<WeighingPhotoDto?> Handle(GetWeighingPhotoByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : WeighingPhotoDto.From(entity);
    }
}

/// <summary>WeighingPhoto kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListWeighingPhotoQuery : PagedRequest, IQuery<PagedResult<WeighingPhotoDto>>
{
    /// <summary>Verilirse yalnızca WeighingId değeri buna eşit kayıtlar döner (<c>?weighingid=...</c>).</summary>
    public Guid? WeighingId { get; set; }

    /// <summary>Verilirse yalnızca Stage değeri buna eşit kayıtlar döner (<c>?stage=...</c>).</summary>
    public WeighingPhotoStage? Stage { get; set; }
}

internal sealed class ListWeighingPhotoHandler : IQueryHandler<ListWeighingPhotoQuery, PagedResult<WeighingPhotoDto>>
{
    private readonly IRepository<WeighingPhoto> _repository;

    public ListWeighingPhotoHandler(IRepository<WeighingPhoto> repository) => _repository = repository;

    public async Task<PagedResult<WeighingPhotoDto>> Handle(ListWeighingPhotoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.WeighingId is { } filterWeighingId)
                {
                    query = query.Where(x => x.WeighingId == filterWeighingId);
                }

                if (request.Stage is { } filterStage)
                {
                    query = query.Where(x => x.Stage == filterStage);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike((x.CameraName ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike(x.Url, $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<WeighingPhotoDto>
        {
            Items = items.Select(WeighingPhotoDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
