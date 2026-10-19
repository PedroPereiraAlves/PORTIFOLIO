using Microsoft.EntityFrameworkCore;
using PedroHub.Domain.Entities;

namespace PedroHub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Projeto> Projetos { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
