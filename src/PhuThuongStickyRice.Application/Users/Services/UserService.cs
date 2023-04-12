using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Repositories;
using System;

namespace PhuThuongStickyRice.Application.Users.Services
{
    public class UserService : CrudService<User>, IUserService
    {
        public UserService(IRepository<User, Guid> userRepository, IDomainEvents domainEvents)
            : base(userRepository, domainEvents)
        {
        }
    }
}
