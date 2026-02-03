using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DAL.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetOne(int id);
        Task<IQueryable<T>> GetAll();
        IQueryable<T> Find(Expression<Func<T, bool>> predicate);
        Task Add(T obj);
        void Delete(T obj);
        void Update(T obj);
    }
}