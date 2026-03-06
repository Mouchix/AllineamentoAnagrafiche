using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class UpsertService<TEntity, TDto>
    where TEntity : class, DbInterface, new()
    where TDto : AnagraficheDto
{
    private readonly AnagraficheContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public UpsertService(AnagraficheContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public int Upsert(TDto dto, Expression<Func<TEntity, bool>> filter, Action<TEntity>? mapExtraFields = null)
    {
        var entity = _dbSet.FirstOrDefault(filter);

        if (entity == null)
        {
            entity = new TEntity { Istat = dto.CodiceISTAT };
            _dbSet.Add(entity);
        }

        entity.Descrizione = dto.Descrizione;
        entity.InizioValidita = dto.InizioValidita;
        entity.FineValidita = dto.FineValidita;

        mapExtraFields?.Invoke(entity);

        _context.SaveChanges();
        return entity.Codice;
    }
}