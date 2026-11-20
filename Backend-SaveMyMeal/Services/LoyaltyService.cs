using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.DTOs.Loyalty;
using Backend_SaveMyMeal.Models.Entities;

namespace Backend_SaveMyMeal.Services
{
    public interface ILoyaltyService
    {
        Task<LoyaltyAccount> GetOrCreateAccountAsync(int userId);
        Task<int> GetPointsBalanceAsync(int userId);
        Task<LoyaltyAccount> AddPointsAsync(int userId, int points);
        Task<PointsRewardResultDto> RedeemPointsAsync(int userId, int pointsToRedeem);
        Task<LoyaltyAccountDto?> GetAccountDtoByUserIdAsync(int userId);
    }

    public class LoyaltyService : ILoyaltyService
    {
        private readonly AppDbContext _context;
        private const int PointsPerEuroDiscount = 20; // 20 points = 1€ de réduction

        public LoyaltyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LoyaltyAccount> GetOrCreateAccountAsync(int userId)
        {
            var account = await _context.LoyaltyAccounts
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (account == null)
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    throw new KeyNotFoundException($"Utilisateur avec l'ID {userId} introuvable.");
                }

                account = new LoyaltyAccount
                {
                    UserId = userId,
                    SavePointsBalance = 50 // Bonus de bienvenue
                };

                _context.LoyaltyAccounts.Add(account);
                await _context.SaveChangesAsync();
                await _context.Entry(account).Reference(l => l.User).LoadAsync();
            }

            return account;
        }

        public async Task<int> GetPointsBalanceAsync(int userId)
        {
            var account = await GetOrCreateAccountAsync(userId);
            return account.SavePointsBalance;
        }

        public async Task<LoyaltyAccount> AddPointsAsync(int userId, int points)
        {
            if (points <= 0)
                throw new ArgumentException("Le nombre de points à ajouter doit être supérieur à zéro.");

            var account = await GetOrCreateAccountAsync(userId);
            account.SavePointsBalance += points;
            await _context.SaveChangesAsync();

            return account;
        }

        public async Task<PointsRewardResultDto> RedeemPointsAsync(int userId, int pointsToRedeem)
        {
            if (pointsToRedeem < PointsPerEuroDiscount)
            {
                return new PointsRewardResultDto
                {
                    Success = false,
                    Message = $"Le minimum de points à convertir est de {PointsPerEuroDiscount} points (1€)."
                };
            }

            var account = await GetOrCreateAccountAsync(userId);

            if (account.SavePointsBalance < pointsToRedeem)
            {
                return new PointsRewardResultDto
                {
                    Success = false,
                    Message = $"Solde insuffisant. Vous disposez de {account.SavePointsBalance} points.",
                    RemainingBalance = account.SavePointsBalance
                };
            }

            // Calcul de la remise par tranches
            int usablePoints = (pointsToRedeem / PointsPerEuroDiscount) * PointsPerEuroDiscount;
            decimal discount = (decimal)usablePoints / PointsPerEuroDiscount;

            account.SavePointsBalance -= usablePoints;
            await _context.SaveChangesAsync();

            return new PointsRewardResultDto
            {
                Success = true,
                Message = $"Succès ! {usablePoints} points convertis en une réduction de {discount:C2}.",
                PointsRedeemed = usablePoints,
                DiscountAmount = discount,
                RemainingBalance = account.SavePointsBalance
            };
        }

        public async Task<LoyaltyAccountDto?> GetAccountDtoByUserIdAsync(int userId)
        {
            var account = await _context.LoyaltyAccounts
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (account == null)
            {
                account = await GetOrCreateAccountAsync(userId);
            }

            return new LoyaltyAccountDto
            {
                Id = account.Id,
                UserId = account.UserId,
                Username = account.User?.Username ?? string.Empty,
                SavePointsBalance = account.SavePointsBalance
            };
        }
    }
}
