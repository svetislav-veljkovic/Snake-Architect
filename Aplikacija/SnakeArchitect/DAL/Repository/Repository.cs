using DAL.Repository.IRepository;
using DAL.DataContext; 
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        
        protected readonly SnakeArchitectContext _context;

        public Repository(SnakeArchitectContext context)
        {
            this._context = context;
        }
        public async Task<T> GetOne(int id)
        {
            var obj = await this._context.Set<T>().FindAsync(id);
            if (obj == null)
            {
                throw new Exception("Object with this ID dosent exists");
            }
            return obj;
        }
        public async Task<IQueryable<T>> GetAll()
        {
            return (IQueryable<T>)await _context.Set<T>().ToListAsync();
        }
        public virtual IQueryable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _context.Set<T>().Where(predicate);
        }
        public async Task Add(T obj)
        {
            await this._context.Set<T>().AddAsync(obj);
        }
        public void Delete(T obj)
        {
            this._context.Set<T>().Remove(obj);
        }
        public void Update(T obj)
        {
            this._context.Set<T>().Update(obj);
        }

    }
}