using MediatR;

namespace POS.Application.Settings.Queries.GetStoreName;

public record GetStoreNameQuery : IRequest<StoreNameDto>;

public record StoreNameDto(string StoreName);
