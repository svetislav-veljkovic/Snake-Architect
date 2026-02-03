using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
 
        public class ChatRepository : Repository<Chat>, IChatRepository
        {
            private readonly SnakeArchitectContext _db;

            public ChatRepository(SnakeArchitectContext db) : base(db)
            {
                _db = db;
            }

        }
    }
