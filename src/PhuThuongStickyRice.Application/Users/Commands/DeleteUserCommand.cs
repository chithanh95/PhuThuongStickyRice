﻿using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Users.Commands
{
    public class DeleteUserCommand : ICommand
    {
        public User User { get; set; }
    }

    internal class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
        {
            _userRepository.Delete(command.User);
            await _userRepository.UnitOfWork.SaveChangesAsync();
        }
    }
}
