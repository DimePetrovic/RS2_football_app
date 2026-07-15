namespace Comeback.BuildingBlocks.Application.Messaging;

using MediatR;

public interface IQuery<TResponse> : IRequest<TResponse> { }
