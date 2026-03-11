using AllineamentoAnagrafiche.Models;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AllineamentoAnagrafiche.Services
{
    public class RemoveService<TEntity, TChild>
        where TEntity : class, DbInterface
        where TChild : class, DbInterface
    {
        private readonly AnagraficheContext _context;
        private readonly DbSet<TEntity> _entitySet;
        private readonly DbSet<TChild> _childSet;

        public RemoveService(AnagraficheContext context)
        {
            _context = context;
            _entitySet = _context.Set<TEntity>();
            _childSet = _context.Set<TChild>();
        }

        public string Remove(bool forzaCancellazione, Expression<Func<TEntity, bool>> searchFilter, 
            Func<TEntity, Expression<Func<TChild, bool>>> referenceFilterFactory) 
        {
            var entity = _entitySet.FirstOrDefault(searchFilter);

            if (entity == null)
            {
                return "AE: elemento non presente nel DB";
            }

            var referenceFilter = referenceFilterFactory(entity);

            bool referenziato = _childSet.Any(referenceFilter);

            if (referenziato)
            {
                if (!forzaCancellazione)
                {
                    return "AE: elemento referenziato nel DB";
                }
            }

            _entitySet.Remove(entity);
            _context.SaveChanges();
            return "AA";
        }

        public string Remove(bool forzaCancellazione, Expression<Func<TEntity, bool>> searchFilter)
        {
            var entity = _entitySet.FirstOrDefault(searchFilter);
            if (entity == null)
            {
                return "AE: elemento non presente nel DB";
            }

            _entitySet.Remove(entity);
            _context.SaveChanges();
            return "AA";
        }

    }
}
