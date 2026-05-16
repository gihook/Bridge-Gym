using BridgeGym.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BridgeGym.Data;

public class BridgeGymContext : IdentityDbContext
{
    public BridgeGymContext(DbContextOptions<BridgeGymContext> options)
        : base(options) { }

    public DbSet<GameSession> GameSessions { get; set; } = null!;
}
