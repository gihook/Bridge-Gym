using Microsoft.EntityFrameworkCore;
using BridgeGym.Models;

namespace BridgeGym.Data;

public class BridgeGymContext : DbContext
{
    public BridgeGymContext(DbContextOptions<BridgeGymContext> options)
        : base(options)
    {
    }

    public DbSet<GameSession> GameSessions { get; set; } = null!;
}
