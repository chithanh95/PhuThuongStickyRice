using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Roles.Commands
{
    public class AddUpdateRoleCommand : ICommand
    {
        public Role Role { get; set; }
    }

    internal class AddUpdateRoleCommandHandler : ICommandHandler<AddUpdateRoleCommand>
    {
        private readonly IRoleRepository _roleRepository;

        public AddUpdateRoleCommandHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task HandleAsync(AddUpdateRoleCommand command, CancellationToken cancellationToken = default)
        {
            await _roleRepository.AddOrUpdateAsync(command.Role);
            await _roleRepository.UnitOfWork.SaveChangesAsync();
        }
    }
}
