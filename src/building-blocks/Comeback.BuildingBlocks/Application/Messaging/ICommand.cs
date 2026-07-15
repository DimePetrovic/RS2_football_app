namespace Comeback.BuildingBlocks.Application.Messaging;

using MediatR;

public interface ICommand : IRequest { }

public interface ICommand<TResponse> : IRequest<TResponse> { }
