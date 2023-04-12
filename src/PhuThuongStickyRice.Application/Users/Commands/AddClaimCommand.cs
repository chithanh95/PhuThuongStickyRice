using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Users.Commands
{
    public class AddClaimCommand : ICommand
    {
        public User User { get; set; }
        public UserClaim Claim { get; set; }
    }

    internal class AddClaimCommandHandler : ICommandHandler<AddClaimCommand>
    {
        private readonly IUserRepository _userRepository;

        public AddClaimCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task HandleAsync(AddClaimCommand command, CancellationToken cancellationToken = default)
        {
            command.User.Claims.Add(command.Claim);
            await _userRepository.AddOrUpdateAsync(command.User);
            await _userRepository.UnitOfWork.SaveChangesAsync();
        }
    }
}
